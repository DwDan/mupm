using MUProcessMonitor.Models;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Forms;

public class ConfigurationTelegramForm : Form
{
    private TextBox txtBotToken, txtChatId;
    private Button btnSave;
    private readonly string configFilePath = "config.dat";
    private NotifyIcon trayIcon;

    public ConfigurationTelegramForm(NotifyIcon _trayIcon)
    {
        Text = "Configuration";
        Width = 400;
        Height = 200;

        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string resourcePath = Path.Combine(basePath, "Resources", "icon_mupm.ico");
        Icon = new Icon(resourcePath);

        trayIcon = _trayIcon;

        Label lblBotToken = new Label() { Text = "Telegram Bot Token:", Left = 10, Top = 20 };
        txtBotToken = new TextBox() { Left = 130, Top = 20, Width = 230 };

        Label lblChatId = new Label() { Text = "Telegram Chat ID:", Left = 10, Top = 60 };
        txtChatId = new TextBox() { Left = 130, Top = 60, Width = 230 };

        btnSave = new Button() { Text = "Save", Left = 150, Top = 100 };
        btnSave.Click += onSave;

        Controls.AddRange(new Control[] { lblBotToken, txtBotToken, lblChatId, txtChatId, btnSave });

        LoadConfiguration();
    }

    private void onSave(object? s, EventArgs e)
    {
        SetConfiguration();
        SaveConfiguration();

        if (SendTestNotification())
        {
            trayIcon.ShowBalloonTip(5000, "Success", "Configuration saved and test notification sent successfully!", ToolTipIcon.Info);
            Close();
        }
        else
        {
            trayIcon.ShowBalloonTip(5000, "Error", "Failed to send test notification. Please check the Telegram configuration.", ToolTipIcon.Error);
        }
    }

    private bool SendTestNotification()
    {
        try
        {
            var telegramService = new Services.TelegramService(trayIcon);
            return telegramService.QueueNotification("Test Notification", "This is a test notification to validate the configuration.");
        }
        catch (Exception ex)
        {
            trayIcon.ShowBalloonTip(5000, "Notification Sending Error", $"Error sending test notification: {ex.Message}", ToolTipIcon.Error);
            return false;
        }
    }

    private void LoadConfiguration()
    {
        if (File.Exists(configFilePath))
        {
            try
            {
                var encryptedData = File.ReadAllBytes(configFilePath);
                if (encryptedData.Length == 0)
                    GetConfiguration();

                var decryptedData = EncryptionService.Decrypt(encryptedData);
                var configParts = decryptedData.Split(';');

                if (configParts.Length == 2)
                {
                    Configuration.BotToken = configParts[0];
                    Configuration.ChatId = configParts[1];

                    GetConfiguration();

                    trayIcon.ShowBalloonTip(5000, "Success", "Configuration loaded successfully!", ToolTipIcon.Info);
                }
                else
                {
                    trayIcon.ShowBalloonTip(5000, "Error", "Configuration file is invalid or corrupted.", ToolTipIcon.Error);
                }
            }
            catch (Exception ex)
            {
                trayIcon.ShowBalloonTip(5000, "Error", $"Failed to load configuration: {ex.Message}", ToolTipIcon.Error);
            }
        }
        else
        {
            trayIcon.ShowBalloonTip(5000, "Info", "No configuration file found.", ToolTipIcon.Info);
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            var configData = $"{Configuration.BotToken};{Configuration.ChatId}";

            var encryptedData = EncryptionService.Encrypt(configData);
            if (encryptedData.Length > 0)
            {
                File.WriteAllBytes(configFilePath, encryptedData);
                trayIcon.ShowBalloonTip(5000, "Success", "Configuration file saved successfully!", ToolTipIcon.Info);
            }
            else
            {
                trayIcon.ShowBalloonTip(5000, "Error", "Failed to encrypt configuration data.", ToolTipIcon.Error);
            }
        }
        catch (Exception ex)
        {
            trayIcon.ShowBalloonTip(5000, "Error", $"Failed to save configuration: {ex.Message}", ToolTipIcon.Error);
        }
    }

    private void GetConfiguration()
    {
        txtBotToken.Text = Configuration.BotToken;
        txtChatId.Text = Configuration.ChatId;
    }

    private void SetConfiguration()
    {
        Configuration.BotToken = txtBotToken.Text;
        Configuration.ChatId = txtChatId.Text;
    }
}