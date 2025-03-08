using MUProcessMonitor.Helpers;
using MUProcessMonitor.Models;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Manager
{
    public class FindBombManager
    {
        private static readonly Lazy<FindBombManager> _instance = new(() => new FindBombManager());
        public static FindBombManager Instance => _instance.Value;

        public Bitmap EmptyCell = BitmapHelper.LoadBitmap("FindBomb", "empty_cell.png")!;

        public Dictionary<int, Bitmap> NumberIcons = new Dictionary<int, Bitmap>
        {
            { 1, BitmapHelper.LoadBitmap("FindBomb", "number1.png")! },
            { 2, BitmapHelper.LoadBitmap("FindBomb", "number2.png")! },
            { 3, BitmapHelper.LoadBitmap("FindBomb", "number3.png")! },
            { 4, BitmapHelper.LoadBitmap("FindBomb", "number4.png")! },
            { 5, BitmapHelper.LoadBitmap("FindBomb", "number5.png")! },
            { 6, BitmapHelper.LoadBitmap("FindBomb", "number6.png")! },
            { 7, BitmapHelper.LoadBitmap("FindBomb", "number7.png")! },
            { 8, BitmapHelper.LoadBitmap("FindBomb", "number8.png")! }
        };

        private HelperMonitorService _helperMonitorService;
        private readonly NotifyIcon _trayIcon;

        private const int GridSize = 8;
        private const int CellSize = 50;

        private readonly CellState[,] _cellStates = new CellState[GridSize, GridSize];
        private readonly int[,] _cellNumbers = new int[GridSize, GridSize];
        private readonly Dictionary<(int, int), float> _riskMap = new();
        private Rectangle _gameRegion;

        public FindBombManager()
        {
            _helperMonitorService = new HelperMonitorService();
            _trayIcon = TrayIconManager.Instance;
        }

        public Bitmap? CaptureScreen(int hWnd)
        {
            var windowRect = _helperMonitorService.GetWindowRectangle(hWnd);
            return _helperMonitorService.CaptureScreen(windowRect);
        }

        public Bitmap? LoadGameCapture(int hWnd)
        {
            var screenshot = CaptureScreen(hWnd);

            if (screenshot != null)
            {
                var initialGame = BitmapHelper.LoadBitmap("FindBomb", "findbomb.png")!;

                if (!_helperMonitorService.IsTemplateVisible(screenshot, initialGame))
                {
                    _trayIcon.ShowBalloonTip(5000, "Error", "Unable to capture the game area", ToolTipIcon.Error);
                    return null;
                }

                _gameRegion = _helperMonitorService.FindSourceRegion(screenshot, initialGame);

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

            var screenshot = _helperMonitorService.CaptureScreen(_gameRegion);
            if (screenshot == null)
            {
                _trayIcon.ShowBalloonTip(5000, "Error", "Unable to capture the game area", ToolTipIcon.Error);
                return BitmapHelper.LoadBitmap("FindBomb", "gameRegion.png");
            }

            var nextStepImage = new Bitmap(screenshot);
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

                        if (IsNumberCell(cellBitmap, out int number))
                        {
                            _cellStates[row, col] = CellState.Number;
                            _cellNumbers[row, col] = number;
                            g.DrawRectangle(numberPen, cellRect);
                            g.DrawString(number.ToString(), new Font("Arial", 10), Brushes.Blue, cellRect.Location);
                        }
                        else if (IsEmptyCell(cellBitmap))
                        {
                            _cellStates[row, col] = CellState.Empty;
                            g.DrawRectangle(emptyPen, cellRect);
                        }
                        else
                        {
                            _cellStates[row, col] = CellState.Unknown;
                        }
                    }
                }

                CalculateRiskMap();

                MarkPossibleBombs(g, possibleBombPen);

                (int nextRow, int nextCol) = GetLowestRiskMove();
                if (nextRow >= 0 && nextCol >= 0)
                {
                    var nextRect = new Rectangle(nextCol * CellSize, nextRow * CellSize, CellSize, CellSize);
                    g.FillRectangle(Brushes.LimeGreen, nextRect);
                }
            }

            return nextStepImage;
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
            float totalRisk = 0f;
            int consideredNumbers = 0;

            foreach (var (nRow, nCol) in GetAdjacentCells(row, col, CellState.Number))
            {
                int bombs = _cellNumbers[nRow, nCol];
                int possibleBombs = GetAdjacentCells(nRow, nCol, CellState.PossibleBomb).Count;
                int unknowns = GetAdjacentCells(nRow, nCol, CellState.Unknown).Count;

                if (unknowns > 0)
                {
                    float risk = (bombs - possibleBombs) / (float)unknowns;
                    totalRisk += risk;
                    consideredNumbers++;
                }
            }

            return consideredNumbers > 0 ? totalRisk / consideredNumbers : 1f;
        }

        private (int, int) GetLowestRiskMove()
        {
            return _riskMap.OrderBy(kvp => kvp.Value).FirstOrDefault().Key;
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

                        if (unknownCells.Count == bombCount)
                        {
                            foreach (var (bombRow, bombCol) in unknownCells)
                            {
                                _cellStates[bombRow, bombCol] = CellState.PossibleBomb;
                                Rectangle bombRect = new Rectangle(bombCol * CellSize, bombRow * CellSize, CellSize, CellSize);
                                g.DrawRectangle(possibleBombPen, bombRect);
                            }
                        }
                    }
                }
            }
        }

        private bool IsEmptyCell(Bitmap cellBitmap)
        {
            return _helperMonitorService.IsTemplateVisible(cellBitmap, EmptyCell);
        }

        private bool IsNumberCell(Bitmap cellBitmap, out int number)
        {
            foreach (var kvp in NumberIcons)
            {
                if (_helperMonitorService.IsTemplateVisible(cellBitmap, kvp.Value))
                {
                    number = kvp.Key;
                    return true;
                }
            }
            number = 0;
            return false;
        }
    }
}
