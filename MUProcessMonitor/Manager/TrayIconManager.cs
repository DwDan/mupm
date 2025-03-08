using MUProcessMonitor.Helpers;

namespace MUProcessMonitor.Manager
{
    public class TrayIconManager
    {
        private static readonly Lazy<NotifyIcon> _instance = new(() =>
        {
            var trayIcon = new NotifyIcon
            {
                Icon = BitmapHelper.LoadIcon("icon_mupm.ico"),
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
