using System.Collections.Concurrent;
using System.Net.Http.Json;
using MUProcessMonitor.Manager;
using MUProcessMonitor.Models;

namespace MUProcessMonitor.Services;

public class TelegramService : IDisposable
{
    private static readonly HttpClient httpClient;
    private NotifyIcon trayIcon;
    private ConcurrentQueue<(string Title, string Message)> messageQueue = new();
    private CancellationTokenSource cancellationTokenSource = new();

    static TelegramService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                           System.Security.Authentication.SslProtocols.Tls13
        };

        httpClient = new HttpClient(handler);
    }

    public TelegramService()
    {
        this.trayIcon = TrayIconManager.Instance;

        Task.Run(() => MonitorMessageQueue(cancellationTokenSource.Token), cancellationTokenSource.Token);
    }

    public bool QueueNotification(string title, string message)
    {
        messageQueue.Enqueue((title, message));
        return true;
    }

    private async Task MonitorMessageQueue(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (messageQueue.TryPeek(out var messageData) && await IsInternetAvailableAsync())
            {
                if (await SendNotificationAsync(messageData.Title, messageData.Message))
                {
                    messageQueue.TryDequeue(out _);
                }
            }

            await Task.Delay(5000, cancellationToken);
        }
    }

    private async Task<bool> IsInternetAvailableAsync()
    {
        try
        {
            using var response = await httpClient.GetAsync("https://www.google.com");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> SendNotificationAsync(string title, string message)
    {
        var botToken = Configuration.BotToken;
        var chatId = Configuration.ChatId;
        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";

        var content = JsonContent.Create(new
        {
            chat_id = chatId,
            text = $"{title}: {message}"
        });

        try
        {
            var response = await httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                trayIcon.ShowBalloonTip(5000, "Telegram Error", $"Failed to send notification: {response.StatusCode}", ToolTipIcon.Error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            trayIcon.ShowBalloonTip(5000, "Telegram Error", $"Error: {ex.Message}", ToolTipIcon.Error);
            return false;
        }
    }

    public void Dispose()
    {
        cancellationTokenSource.Cancel();
        httpClient?.Dispose();
    }
}
