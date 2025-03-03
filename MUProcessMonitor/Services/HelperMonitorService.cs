using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using MUProcessMonitor.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace MUProcessMonitor.Services
{
    public class HelperMonitorService
    {
        private static double threshold = 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public Rectangle GetWindowRectangle(int windowHandle)
        {
            if (GetWindowRect((IntPtr)windowHandle, out RECT rect))
                return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

            return Rectangle.Empty;
        }

        public bool IsHelperInactive(int windowHandle, Rectangle windowRect)
        {
            var screenshot = CaptureScreen(windowRect);

            return IsIconVisible(screenshot, GetIcon("play_icon.png"), threshold) || 
                IsIconVisible(screenshot, GetIcon("helper_off.png"), threshold);
        }

        private Bitmap? GetIcon(string iconName)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string resourcePath = Path.Combine(basePath, "Resources", iconName);

            if (File.Exists(resourcePath))
                return new Bitmap(resourcePath);

            throw new FileNotFoundException($"Ícone não encontrado: {resourcePath}");
        }

        public Bitmap CaptureScreen(Rectangle region)
        {
            Bitmap bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(region.Left, region.Top, 0, 0, region.Size, CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }

        private bool IsIconVisible(Bitmap screenshot, Bitmap? icon, double threshold)
        {
            if(icon is null)
                return false;

            using Mat source = BitmapConverter.ToMat(screenshot);
            using Mat template = BitmapConverter.ToMat(icon);
            using Mat result = new Mat();

            Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);

            return maxVal >= threshold;
        }
    }
}
