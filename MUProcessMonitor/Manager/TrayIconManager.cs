namespace MUProcessMonitor.Manager
{
    public class TrayIconManager
    {
        private static readonly Lazy<NotifyIcon> _instance = new(() =>
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string resourcePath = Path.Combine(basePath, "Resources", "icon_mupm.ico");

            var trayIcon = new NotifyIcon
            {
                Icon = new Icon(resourcePath),
                Visible = true,
                Text = "Window Monitor",
                ContextMenuStrip = new ContextMenuStrip()
            };

            return trayIcon;
        });

        public static NotifyIcon Instance => _instance.Value;

        public static void ShowBalloonTip(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int duration = 5000)
        {
            Instance.ShowBalloonTip(duration, title, message, icon);
        }

        public static void AddContextMenuItem(string text, EventHandler onClick)
        {
            if (Instance.ContextMenuStrip != null)
            {
                Instance.ContextMenuStrip.Items.Add(text, null, onClick);
            }
        }

        public static void ClearContextMenu()
        {
            if (Instance.ContextMenuStrip != null)
            {
                Instance.ContextMenuStrip?.Items.Clear();
            }
        }
    }
}
