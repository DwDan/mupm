using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using MUProcessMonitor.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace MUProcessMonitor.Services
{
    public class HelperMonitorService
    {
        private static readonly double Threshold = 1;
        private static readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string ResourcesPath = Path.Combine(BasePath, "Resources");

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public Rectangle GetWindowRectangle(int windowHandle)
        {
            return GetWindowRect((IntPtr)windowHandle, out RECT rect)
                ? new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top)
                : Rectangle.Empty;
        }

        public bool IsHelperInactive(int windowHandle, Rectangle windowRect)
        {
            var screenshot = CaptureScreen(windowRect);
            return IsIconVisible(screenshot, LoadIcon("play_icon.png"), Threshold, true) ||
                   IsIconVisible(screenshot, LoadIcon("helper_off.png"), Threshold, false);
        }

        private Bitmap? LoadIcon(string iconName)
        {
            string resourcePath = Path.Combine(ResourcesPath, iconName);
            if (File.Exists(resourcePath))
                return new Bitmap(resourcePath);

            return null;
        }

        public Bitmap CaptureScreen(Rectangle region)
        {
            if (region.Width <= 0 || region.Height <= 0)
                return new Bitmap(1, 1);

            Bitmap bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(region.Left, region.Top, 0, 0, region.Size, CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }

        private bool IsIconVisible(Bitmap screenshot, Bitmap? icon, double threshold, bool isTopRegion)
        {
            if (icon is null || screenshot == null || screenshot.Width == 0 || screenshot.Height == 0)
                return false;

            using Mat source = BitmapConverter.ToMat(screenshot);
            using Mat template = BitmapConverter.ToMat(icon);

            if (source.Empty() || template.Empty())
                return false;

            using Mat result = new Mat();

            int width = source.Width;
            int height = source.Height;

            if (width == 0 || height == 0)
                return false;

            int roiWidth = width / 2;
            int roiHeight = isTopRegion ? height / 6 : height / 7;

            if (roiWidth <= 0 || roiHeight <= 0)
                return false;

            int roiX = 0;
            int roiY = isTopRegion ? 0 : height - roiHeight;

            Rect roi = new Rect(roiX, roiY, roiWidth, roiHeight);
            using Mat croppedSource = new Mat(source, roi);

            if (croppedSource.Empty())
                return false;

            Cv2.MatchTemplate(croppedSource, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);

            return maxVal >= threshold;
        }
    }
}
