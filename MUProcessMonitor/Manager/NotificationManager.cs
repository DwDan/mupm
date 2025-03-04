using global::MUProcessMonitor.Services;

namespace MUProcessMonitor.Manager;

public class NotificationManager
{
    private NotifyIcon _trayIcon;
    private TelegramService _telegramService;

    private static readonly Lazy<NotificationManager> _instance = new(() => new NotificationManager());
    public static NotificationManager Instance => _instance.Value;

    public NotificationManager()
    {
        _trayIcon = TrayIconManager.Instance;
        _telegramService = new TelegramService();
    }

    public void ShowBalloonTip(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int duration = 5000)
    {
        _trayIcon.ShowBalloonTip(duration, title, message, icon);
    }

    public void SendNotification(string title, string message)
    {
        _telegramService.QueueNotification(title, message);
    }

    public void SendNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int duration = 5000)
    {
        SendNotification(title, message);

        ShowBalloonTip(title, message, icon, duration);
    }

    public bool SendTestNotification()
    {
        return _telegramService.QueueNotification("Test Notification", "Validating the configuration.");
    }
}

