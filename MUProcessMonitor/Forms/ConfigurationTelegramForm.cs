using MUProcessMonitor.Models;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Forms;

public class ConfigurationTelegramForm : Form
{
    private TextBox txtBotToken, txtChatId, txtThreadSleep;
    private CheckBox chkUseAlarm;
    private ComboBox cmbAlarmSound;
    private Button btnSave;
    private readonly string configFilePath = "config.dat";
    private NotifyIcon trayIcon;
    private AlarmService alarmService;

    public ConfigurationTelegramForm(NotifyIcon _trayIcon)
    {
        Text = "Configuration";
        Width = 400;
        Height = 320;

        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string resourcePath = Path.Combine(basePath, "Resources", "icon_mupm.ico");
        Icon = new Icon(resourcePath);

        trayIcon = _trayIcon;
        alarmService = new AlarmService();

        Label lblBotToken = new Label() { Text = "Telegram Bot Token:", Left = 10, Top = 20 };
        txtBotToken = new TextBox() { Left = 130, Top = 20, Width = 230 };

        Label lblChatId = new Label() { Text = "Telegram Chat ID:", Left = 10, Top = 60 };
        txtChatId = new TextBox() { Left = 130, Top = 60, Width = 230 };

        chkUseAlarm = new CheckBox() { Text = "Use Alarm", Left = 130, Top = 100, Width = 230 };

        Label lblAlarmSound = new Label() { Text = "Alarm Sound:", Left = 10, Top = 140 };
        cmbAlarmSound = new ComboBox() { Left = 130, Top = 140, Width = 230 };
        cmbAlarmSound.Items.AddRange(new string[] { "None", "alert_1.mp3", "alert_2.mp3", "alert_3.mp3", "alert_4.mp3",
            "alert_5.mp3", "alert_6.mp3", "alert_7.mp3", "alert_8.mp3", "alert_9.mp3", "alert_10.mp3" });
        cmbAlarmSound.SelectionChangeCommitted += (s, e) => PlaySelectedSound();

        Label lblThreadSleep = new Label() { Text = "Monitor Interval (ms):", Left = 10, Top = 180 };
        txtThreadSleep = new TextBox() { Left = 130, Top = 180, Width = 230, Text = Configuration.ThreadSleepTime.ToString() };

        btnSave = new Button() { Text = "Save", Left = 150, Top = 220 };
        btnSave.Click += onSave;

        Controls.AddRange(new Control[] { lblBotToken, txtBotToken, lblChatId, txtChatId, chkUseAlarm, lblAlarmSound, cmbAlarmSound, lblThreadSleep, txtThreadSleep, btnSave });

        LoadConfiguration();
    }

    private async void PlaySelectedSound()
    {
        string selectedSound = cmbAlarmSound.SelectedItem?.ToString() ?? "None";
        if (selectedSound != "None")
            await alarmService.Start(selectedSound);
        else
            alarmService.Stop();
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

        alarmService.Stop();
    }

    private bool SendTestNotification()
    {
        try
        {
            var telegramService = new TelegramService(trayIcon);
            return telegramService.QueueNotification("Test Notification", "This is a test notification to validate the configuration.");
        }
        catch (Exception ex)
        {
            trayIcon.ShowBalloonTip(5000, "Notification Sending Error", $"Error sending test notification: {ex.Message}", ToolTipIcon.Error);
            return false;
        }
    }

    public void LoadConfiguration()
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

                if (configParts.Length == 5)
                {
                    Configuration.BotToken = configParts[0];
                    Configuration.ChatId = configParts[1];
                    Configuration.UseAlarm = bool.Parse(configParts[2]);
                    Configuration.AlarmSound = configParts[3];
                    Configuration.ThreadSleepTime = int.Parse(configParts[4]);
                }
                else
                {
                    trayIcon.ShowBalloonTip(5000, "Error", "Configuration file is invalid or corrupted.", ToolTipIcon.Error);
                }

                GetConfiguration();
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
            var configData = $"{Configuration.BotToken};{Configuration.ChatId};{Configuration.UseAlarm};{Configuration.AlarmSound};{Configuration.ThreadSleepTime}";

            var encryptedData = EncryptionService.Encrypt(configData);
            if (encryptedData.Length > 0)
            {
                File.WriteAllBytes(configFilePath, encryptedData);
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
        txtBotToken.Text = Configuration.BotToken ?? "your-bot-token";
        txtChatId.Text = Configuration.ChatId ?? "your-chat-id";
        chkUseAlarm.Checked = Configuration.UseAlarm;
        cmbAlarmSound.SelectedItem = Configuration.AlarmSound ?? "alert_1.mp3";
        txtThreadSleep.Text = Configuration.ThreadSleepTime.ToString();
    }

    private void SetConfiguration()
    {
        Configuration.BotToken = txtBotToken.Text;
        Configuration.ChatId = txtChatId.Text;
        Configuration.UseAlarm = chkUseAlarm.Checked;
        Configuration.AlarmSound = cmbAlarmSound.SelectedItem?.ToString() ?? "None";
        int.TryParse(txtThreadSleep.Text, out int threadSleep);
        Configuration.ThreadSleepTime = threadSleep > 0 ? threadSleep : 60000;
    }
}