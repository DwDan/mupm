using MUProcessMonitor.Helpers;
using MUProcessMonitor.Models;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Manager
{
    public class FindBombManager
    {
        private static readonly Lazy<FindBombManager> _instance = new(() => new FindBombManager());
        public static FindBombManager Instance => _instance.Value;

        private FindBombService _findBombService;
        private readonly NotifyIcon _trayIcon;

        private const int GridSize = 8;
        private const int CellSize = 24;

        private readonly CellState[,] _cellStates = new CellState[GridSize, GridSize];
        private readonly int[,] _cellNumbers = new int[GridSize, GridSize];
        private readonly Dictionary<(int, int), float> _riskMap = new();
        private Rectangle _gameRegion;
        private int _windowHandle;

        public FindBombManager()
        {
            _findBombService = new FindBombService();
            _trayIcon = TrayIconManager.Instance;
        }

        public Bitmap? LoadGameCapture(int hWnd)
        {
            _windowHandle = hWnd;

            var screenshot = _findBombService.CaptureScreen(_windowHandle);

            if (screenshot != null)
            {
                var initialGame = BitmapHelper.LoadBitmap("FindBomb", "findbomb.png")!;

                if (!_findBombService.IsTemplateVisible(screenshot, initialGame))
                {
                    _trayIcon.ShowBalloonTip(5000, "Error", "Unable to capture the game area", ToolTipIcon.Error);
                    return null;
                }

                _gameRegion = _findBombService.FindSourceRegion(screenshot, initialGame);

                if (_gameRegion != Rectangle.Empty)
                {
                    var gameScreenshot = screenshot.Clone(_gameRegion, screenshot.PixelFormat);
                    return new Bitmap(gameScreenshot);
                }
                else
                {
                    _trayIcon.ShowBalloonTip(5000, "Error", "Unable to capture the game area", ToolTipIcon.Error);
                    return null;
                }
            }
            else
            {
                _trayIcon.ShowBalloonTip(5000, "Error", "Unable to capture the game area", ToolTipIcon.Error);
                return null;
            }
        }

        public Bitmap? CalculateNextStep()
        {
            if (_gameRegion == Rectangle.Empty)
                return null;

            var screenshot = _findBombService.CaptureScreen(_windowHandle);

            if (screenshot == null)
            {
                _trayIcon.ShowBalloonTip(5000, "Error", "Unable to capture the game area", ToolTipIcon.Error);
                return BitmapHelper.LoadBitmap("FindBomb", "gameRegion.png");
            }

            var screenshotRegion = _findBombService.CaptureRegion(screenshot, _gameRegion);
            var nextStepImage = new Bitmap(screenshotRegion);
            using (Graphics g = Graphics.FromImage(nextStepImage))
            {
                var emptyPen = new Pen(Color.Gray, 2);
                var possibleBombPen = new Pen(Color.Red, 2);
                var numberPen = new Pen(Color.Blue, 2);
                var nextMovePen = new Pen(Color.LimeGreen, 2);

                _riskMap.Clear();

                for (int row = 0; row < GridSize; row++)
                {
                    for (int col = 0; col < GridSize; col++)
                    {
                        var cellRect = new Rectangle(col * CellSize, row * CellSize, CellSize, CellSize);
                        var cellBitmap = nextStepImage.Clone(cellRect, nextStepImage.PixelFormat);

                        if (_findBombService.IsNumberCell(cellBitmap, out int number))
                        {
                            _cellStates[row, col] = CellState.Number;
                            _cellNumbers[row, col] = number;
                            g.DrawRectangle(numberPen, cellRect);
                        }
                        else if (_findBombService.IsUnknowCell(cellBitmap))
                        {
                            _cellStates[row, col] = CellState.Unknown;
                        }
                        else if (_findBombService.IsEmptyCell(cellBitmap))
                        {
                            _cellStates[row, col] = CellState.Empty;
                            g.DrawRectangle(emptyPen, cellRect);
                        }
                    }
                }

                MarkPossibleBombs(g, possibleBombPen);

                CalculateRiskMap();

                DrawNextMove(g);
            }

            return nextStepImage;
        }

        private void DrawNextMove(Graphics g)
        {
            (int nextRow, int nextCol) = GetLowestRiskMove();
            if (nextRow >= 0 && nextCol >= 0)
            {
                var nextRect = new Rectangle(nextCol * CellSize, nextRow * CellSize, CellSize, CellSize);
                g.FillRectangle(Brushes.LimeGreen, nextRect);
            }
        }

        private void CalculateRiskMap()
        {
            foreach (var (row, col) in GetAllUnknownCells())
            {
                float risk = CalculateRiskForCell(row, col);
                _riskMap[(row, col)] = risk;
            }
        }

        private float CalculateRiskForCell(int row, int col)
        {
            float minRisk = 1f;

            foreach (var (nRow, nCol) in GetAdjacentCells(row, col, CellState.Number))
            {
                int bombs = _cellNumbers[nRow, nCol];
                int possibleBombs = GetAdjacentCells(nRow, nCol, CellState.PossibleBomb).Count;
                var unknowns = GetAdjacentCells(nRow, nCol, CellState.Unknown);

                if (unknowns.Count > 0)
                {
                    int bombsNeeded = bombs - possibleBombs;
                    float risk = bombsNeeded / (float)unknowns.Count;

                    minRisk = Math.Min(minRisk, risk);
                }
            }

            return minRisk;
        }

        private (int, int) GetLowestRiskMove()
        {
            var safeCells = _riskMap
                .Where(kvp => kvp.Value == 0 && _cellStates[kvp.Key.Item1, kvp.Key.Item2] == CellState.Unknown)
                .ToList();

            if (safeCells.Any())
                return safeCells.First().Key;

            return _riskMap
                .Where(kvp => _cellStates[kvp.Key.Item1, kvp.Key.Item2] != CellState.PossibleBomb)
                .OrderBy(kvp => kvp.Value)
                .FirstOrDefault().Key;
        }

        private IEnumerable<(int, int)> GetAllUnknownCells()
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (_cellStates[row, col] == CellState.Unknown)
                    {
                        yield return (row, col);
                    }
                }
            }
        }

        private List<(int, int)> GetAdjacentCells(int row, int col, CellState targetState)
        {
            var adjacentCells = new List<(int, int)>();
            for (int r = row - 1; r <= row + 1; r++)
            {
                for (int c = col - 1; c <= col + 1; c++)
                {
                    if (r >= 0 && r < GridSize && c >= 0 && c < GridSize && (r != row || c != col))
                    {
                        if (_cellStates[r, c] == targetState)
                        {
                            adjacentCells.Add((r, c));
                        }
                    }
                }
            }
            return adjacentCells;
        }

        private void MarkPossibleBombs(Graphics g, Pen possibleBombPen)
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (_cellStates[row, col] == CellState.Number)
                    {
                        var unknownCells = GetAdjacentCells(row, col, CellState.Unknown);
                        var bombCount = _cellNumbers[row, col];
                        var possibleBombs = GetAdjacentCells(row, col, CellState.PossibleBomb).Count;

                        if (bombCount - possibleBombs == unknownCells.Count)
                        {
                            foreach (var (bombRow, bombCol) in unknownCells)
                            {
                                _cellStates[bombRow, bombCol] = CellState.PossibleBomb;
                                Rectangle bombRect = new Rectangle(bombCol * CellSize, bombRow * CellSize, CellSize, CellSize);
                                g.DrawRectangle(possibleBombPen, bombRect);
                            }
                        }

                        if (bombCount == possibleBombs)
                        {
                            foreach (var (safeRow, safeCol) in unknownCells)
                            {
                                _cellStates[safeRow, safeCol] = CellState.Safe;
                            }
                        }
                    }
                }
            }
        }
    }
}
