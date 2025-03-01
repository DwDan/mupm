using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using MUProcessMonitor.Models;

namespace MUProcessMonitor.Services;

public class EmailService
{
    private ConcurrentQueue<(string Subject, string Message)> emailQueue = new();
    private NotifyIcon trayIcon;
    private Thread emailMonitorThread;

    public EmailService(NotifyIcon trayIcon)
    {
        this.trayIcon = trayIcon;
        emailMonitorThread = new Thread(MonitorEmailQueue) { IsBackground = true };
        emailMonitorThread.Start();

        System.Net.ServicePointManager.SecurityProtocol =
            SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
    }

    public bool QueueEmail(string subject, string message)
    {
        if (IsInternetAvailable())
        {
            return SendEmailDirectly(subject, message);
        }
        else
        {
            emailQueue.Enqueue((subject, message));
            trayIcon.ShowBalloonTip(5000, "Email Queued", "Internet not available. Email added to the queue.", ToolTipIcon.Info);
            return false;
        }
    }

    private void MonitorEmailQueue()
    {
        while (true)
        {
            if (emailQueue.TryDequeue(out var emailData) && IsInternetAvailable())
            {
                SendEmailDirectly(emailData.Subject, emailData.Message);
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

    private bool SendEmailDirectly(string subject, string message)
    {
        try
        {
            using (var client = new SmtpClient(Configuration.SMTPServer, Configuration.SMTPPort))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(Configuration.EmailSender, Configuration.EmailPassword);
                client.EnableSsl = Configuration.SMTPEnableSsl;
                client.DeliveryMethod = Configuration.SMTPDeliveryMethod;
                client.Timeout = Configuration.SMTPTimeout;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Configuration.EmailSender),
                    Subject = subject,
                    Body = message
                };

                mailMessage.To.Add(Configuration.EmailRecipient);
                client.Send(mailMessage);

                trayIcon.ShowBalloonTip(5000, "Email Sent", $"Email sent: {subject}", ToolTipIcon.Info);
                return true;
            }
        }
        catch (SmtpException ex)
        {
            trayIcon.ShowBalloonTip(5000, "SMTP Error", $"{ex.StatusCode}: {ex.Message}", ToolTipIcon.Error);
            Console.WriteLine($"SMTP Error: {ex.StatusCode}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            trayIcon.ShowBalloonTip(5000, "Email Sending Error", ex.Message, ToolTipIcon.Error);
            Console.WriteLine($"Email Sending Error: {ex.Message}");
            return false;
        }
    }
}
