using MUProcessMonitor.Helpers;
using MUProcessMonitor.Manager;
using OpenCvSharp.Extensions;
using OpenCvSharp;

namespace MUProcessMonitor.Services
{
    public class FindBombService
    {
        public Bitmap EmptyCell = BitmapHelper.LoadBitmap("FindBomb", "empty_cell.png")!;
        public Bitmap UnknowCell = BitmapHelper.LoadBitmap("FindBomb", "unknow_cell.png")!;

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

        public FindBombService()
        {
            _helperMonitorService = new HelperMonitorService();
            _trayIcon = TrayIconManager.Instance;
        }

        public Bitmap? CaptureScreen(int _windowHandle)
        {
            var windowRect = _helperMonitorService.GetWindowRectangle(_windowHandle);

            return _helperMonitorService.CaptureScreen(windowRect);
        }

        public bool IsEmptyCell(Bitmap cellBitmap)
        {
            return _helperMonitorService.IsTemplateVisible(cellBitmap, EmptyCell);
        }

        public bool IsUnknowCell(Bitmap cellBitmap)
        {
            return _helperMonitorService.IsTemplateVisible(cellBitmap, UnknowCell);
        }

        public bool IsNumberCell(Bitmap cellBitmap, out int number)
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

        public Bitmap CaptureRegion(Bitmap screenshot, Rectangle region)
        {
            if (region.Width <= 0 || region.Height <= 0)
                return new Bitmap(1, 1);

            Rectangle validRegion = Rectangle.Intersect(new Rectangle(System.Drawing.Point.Empty, screenshot.Size), region);

            return screenshot.Clone(validRegion, screenshot.PixelFormat);
        }

        public Rectangle FindSourceRegion(Bitmap screenshot, Bitmap region)
        {
            using Mat source = BitmapConverter.ToMat(screenshot);
            using Mat template = BitmapConverter.ToMat(region);
            using Mat result = new Mat();

            Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal >= 0.95)
                return new Rectangle(maxLoc.X, maxLoc.Y, region.Width, region.Height);

            return Rectangle.Empty;
        }

        public bool IsTemplateVisible(Bitmap sourceImage, Bitmap templateImage)
        {
            return _helperMonitorService.IsTemplateVisible(sourceImage, templateImage);
        }
    }
}
