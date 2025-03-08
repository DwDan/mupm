namespace MUProcessMonitor.Helpers
{
    public class BitmapHelper
    {
        private static readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string ResourcesPath = Path.Combine(BasePath, "Resources");

        public static Bitmap? LoadBitmap(string bitmapName)
        {
            string resourcePath = Path.Combine(ResourcesPath, bitmapName);
            if (File.Exists(resourcePath))
                return new Bitmap(resourcePath);
            return null;
        }

        public static Icon? LoadIcon(string iconName)
        {
            string resourcePath = Path.Combine(ResourcesPath, iconName);
            if (File.Exists(resourcePath))
                return new Icon(resourcePath);
            return null;
        }
    }
}
