using System.Collections.Concurrent;
using System.Net;
using MUProcessMonitor.Models;

namespace MUProcessMonitor.Services;

public class TelegramService
{
    private static readonly HttpClient httpClient = new HttpClient();
    private NotifyIcon trayIcon;
    private ConcurrentQueue<(string Title, string Message)> messageQueue = new();
    private Thread messageMonitorThread;

    public TelegramService(NotifyIcon trayIcon)
    {
        this.trayIcon = trayIcon;
        messageMonitorThread = new Thread(MonitorMessageQueue) { IsBackground = true };
        messageMonitorThread.Start();
    }

    public bool QueueNotification(string title, string message)
    {
        if (IsInternetAvailable())
        {
            return SendNotificationDirectly(title, message);
        }
        else
        {
            messageQueue.Enqueue((title, message));
            trayIcon.ShowBalloonTip(5000, "Notification Queued", "Internet not available. Notification added to the queue.", ToolTipIcon.Info);
            return false;
        }
    }

    private void MonitorMessageQueue()
    {
        while (true)
        {
            if (messageQueue.TryDequeue(out var messageData) && IsInternetAvailable())
            {
                SendNotificationDirectly(messageData.Title, messageData.Message);
            }
            Thread.Sleep(5000);
        }
    }

    private bool IsInternetAvailable()
    {
        try
        {
            using var client = new WebClient();
            using (client.OpenRead("http://www.google.com"))
                return true;
        }
        catch
        {
            return false;
        }
    }

    private bool SendNotificationDirectly(string title, string message)
    {
        var botToken = Configuration.BotToken;
        var chatId = Configuration.ChatId;
        var url = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={chatId}&text={title}: {message}";

        try
        {
            var response = httpClient.GetAsync(url).Result;
            if (!response.IsSuccessStatusCode)
            {
                trayIcon.ShowBalloonTip(5000, "Telegram Error", $"Failed to send notification: {response.StatusCode}", ToolTipIcon.Error);
                return false;
            }
            else
            {
                trayIcon.ShowBalloonTip(5000, "Telegram Notification", $"Notification sent: {title}", ToolTipIcon.Info);
                return true;
            }
        }
        catch (Exception ex)
        {
            trayIcon.ShowBalloonTip(5000, "Telegram Error", $"Error: {ex.Message}", ToolTipIcon.Error);
            return false;
        }
    }
}
