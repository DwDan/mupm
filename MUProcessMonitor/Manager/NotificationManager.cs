using global::MUProcessMonitor.Services;

namespace MUProcessMonitor.Manager;

public class NotificationManager
{
    private NotifyIcon _trayIcon;
    private TelegramService _telegramService;

    public NotificationManager(NotifyIcon trayIcon)
    {
        _trayIcon = trayIcon;
        _telegramService = new TelegramService(trayIcon);
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
}

