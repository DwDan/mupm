using MUProcessMonitor.Services;

namespace MUProcessMonitor.Manager
{
    public class ScreenShotManager
    {
        private static readonly Lazy<ScreenShotManager> _instance = new(() => new ScreenShotManager());
        private readonly ScreenShotService _screenShotService;

        private ScreenShotManager()
        {
            _screenShotService = new ScreenShotService();
        }

        public static ScreenShotManager Instance => _instance.Value;

        public void Clear()
        {
            _screenShotService.Clear();
        }

        public bool ContainsScreenshot(string handle)
        {
            return _screenShotService.ScreenshotCache.ContainsKey(handle);
        }

        public void AddScreenshot(string handle, Bitmap screenshot)
        {
            _screenShotService.ScreenshotCache[handle] = screenshot;
        }

        public void ShowScreenshot(string handle)
        {
            if (!_screenShotService.ScreenshotCache.TryGetValue(handle, out var screenshot)) return;

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string resourcePath = Path.Combine(basePath, "Resources", "icon_mupm.ico");

            var screenshotForm = new Form
            {
                Text = "Screenshot Preview",
                Width = screenshot.Width + 20,
                Height = screenshot.Height + 40,
                StartPosition = FormStartPosition.CenterScreen,
                Icon = new Icon(resourcePath)
            };

            var pictureBox = new PictureBox
            {
                Image = screenshot,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
            };

            screenshotForm.Controls.Add(pictureBox);
            screenshotForm.ShowDialog();
        }
    }
}
