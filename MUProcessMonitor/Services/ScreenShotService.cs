namespace MUProcessMonitor.Services
{
    public class ScreenShotService
    {
        public Dictionary<int, Bitmap> ScreenshotCache;

        public ScreenShotService()
        {
            ScreenshotCache = new Dictionary<int, Bitmap>();
        }

        public void Clear()
        {
            ScreenshotCache.Clear();
        }
    }
}
