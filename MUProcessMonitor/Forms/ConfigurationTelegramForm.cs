using MUProcessMonitor.Manager;
using MUProcessMonitor.Models;

namespace MUProcessMonitor.Forms;

public class ConfigurationTelegramForm : Form
{
    private TextBox txtBotToken = new();
    private TextBox txtChatId = new();
    private TextBox txtThreadSleep = new();
    private CheckBox chkUseAlarm = new();
    private ComboBox cmbAlarmSound = new();
    private Button btnSave = new();

    public NotificationManager NotificationManager;
    public AlarmManager AlarmManager;
    public ConfigurationManager ConfigurationManager;

    public ConfigurationTelegramForm()
    {
        NotificationManager = NotificationManager.Instance;
        AlarmManager = AlarmManager.Instance;
        ConfigurationManager = ConfigurationManager.Instance;

        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string resourcePath = Path.Combine(basePath, "Resources", "icon_mupm.ico");
        Icon = new Icon(resourcePath);

        Text = "Configuration";
        Width = 400;
        Height = 320;

        InitializeComponents();

        LoadConfiguration();
    }

    private void InitializeComponents()
    {
        txtBotToken = new TextBox() { Left = 130, Top = 20, Width = 230 };
        txtChatId = new TextBox() { Left = 130, Top = 60, Width = 230 };
        chkUseAlarm = new CheckBox() { Text = "Use Alarm", Left = 130, Top = 100, Width = 230 };

        cmbAlarmSound = new ComboBox() { Left = 130, Top = 140, Width = 230 };
        cmbAlarmSound.Items.AddRange(new string[] { "None", "alert_1.mp3", "alert_2.mp3", "alert_3.mp3", "alert_4.mp3",
            "alert_5.mp3", "alert_6.mp3", "alert_7.mp3", "alert_8.mp3", "alert_9.mp3", "alert_10.mp3" });
        cmbAlarmSound.SelectionChangeCommitted += (s, e) => PlaySelectedSound();

        txtThreadSleep = new TextBox() { Left = 130, Top = 180, Width = 230 };

        btnSave = new Button() { Text = "Save", Left = 150, Top = 220 };
        btnSave.Click += onSave;

        Controls.AddRange(new Control[]
        {
            new Label() { Text = "Telegram Bot Token:", Left = 10, Top = 20 },
            txtBotToken,
            new Label() { Text = "Telegram Chat ID:", Left = 10, Top = 60 },
            txtChatId,
            chkUseAlarm,
            new Label() { Text = "Alarm Sound:", Left = 10, Top = 140 },
            cmbAlarmSound,
            new Label() { Text = "Monitor Interval (ms):", Left = 10, Top = 180 },
            txtThreadSleep,
            btnSave
        });
    }

    private async void PlaySelectedSound()
    {
        string selectedSound = cmbAlarmSound.SelectedItem?.ToString() ?? "None";

        await AlarmManager.PlaySelectedSound(selectedSound);
    }

    private void onSave(object? sender, EventArgs e)
    {
        SetConfiguration();
        ConfigurationManager.SaveConfiguration();

        if (NotificationManager.SendTestNotification())
        {
            NotificationManager.ShowBalloonTip("Success", "Configuration saved and test notification sent successfully!");

            Close();
        }
        else
        {
            NotificationManager.ShowBalloonTip("Error", "Failed to send test notification.", ToolTipIcon.Error);
        }

        AlarmManager.StopAlarm();
    }

    private void LoadConfiguration()
    {
        ConfigurationManager.LoadConfiguration();

        txtBotToken.Text = Configuration.BotToken;
        txtChatId.Text = Configuration.ChatId;
        chkUseAlarm.Checked = Configuration.UseAlarm;
        cmbAlarmSound.SelectedItem = Configuration.AlarmSound;
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
