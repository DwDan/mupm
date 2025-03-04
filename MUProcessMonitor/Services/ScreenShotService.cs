namespace MUProcessMonitor.Services
{
    public class ScreenShotService
    {
        public Dictionary<string, Bitmap> ScreenshotCache;

        public ScreenShotService()
        {
            ScreenshotCache = new Dictionary<string, Bitmap>();
        }

        public void Clear()
        {
            ScreenshotCache.Clear();
        }
    }
}
