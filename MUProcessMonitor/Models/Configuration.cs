namespace MUProcessMonitor.Models;
using System.Net.Mail;

public static class Configuration
{
    public static double MaxPercentageChange { get; set; } = 30;
    public static string SMTPServer { get; set; } = "smtp.example.com";
    public static int SMTPPort { get; set; } = 587;
    public static string EmailSender { get; set; } = "your-email@example.com";
    public static string EmailPassword { get; set; } = "yourpassword";
    public static string EmailRecipient { get; set; } = "recipient@example.com";
    public static bool SMTPEnableSsl { get; set; } = true;
    public static SmtpDeliveryMethod SMTPDeliveryMethod { get; set; } = SmtpDeliveryMethod.Network;
    public static int SMTPTimeout { get; set; } = 10000;
}
