using System.Net.Mail;
using MUProcessMonitor.Models;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Forms;

public class ConfigurationEmailForm : Form
{
    private NumericUpDown nudMaxPercentage, nudTimeout;
    private TextBox txtSMTP, txtPort, txtEmailSender, txtPassword, txtRecipient;
    private CheckBox chkEnableSsl;
    private ComboBox cmbDeliveryMethod;
    private Button btnSave;
    private readonly string configFilePath = "config.dat";
    private EmailService emailService;
    private NotifyIcon trayIcon;

    public ConfigurationEmailForm(NotifyIcon _trayIcon)
    {
        Text = "Configuration";
        Width = 400;
        Height = 400;

        trayIcon = _trayIcon;
        emailService = new EmailService(trayIcon);

        Label lblMaxPercentage = new Label() { Text = "Max % Change:", Left = 10, Top = 20 };
        nudMaxPercentage = new NumericUpDown() { Left = 130, Top = 20, Width = 50 };

        Label lblSMTP = new Label() { Text = "SMTP Server:", Left = 10, Top = 60 };
        txtSMTP = new TextBox() { Left = 130, Top = 60, Width = 180 };

        Label lblPort = new Label() { Text = "Port:", Left = 10, Top = 90 };
        txtPort = new TextBox() { Left = 130, Top = 90, Width = 50 };

        Label lblEmailSender = new Label() { Text = "Sender Email:", Left = 10, Top = 120 };
        txtEmailSender = new TextBox() { Left = 130, Top = 120, Width = 180 };

        Label lblPassword = new Label() { Text = "Password:", Left = 10, Top = 150 };
        txtPassword = new TextBox() { Left = 130, Top = 150, Width = 180, PasswordChar = '*' };

        Label lblRecipient = new Label() { Text = "Recipient:", Left = 10, Top = 180 };
        txtRecipient = new TextBox() { Left = 130, Top = 180, Width = 180 };

        Label lblEnableSsl = new Label() { Text = "Enable SSL:", Left = 10, Top = 210 };
        chkEnableSsl = new CheckBox() { Left = 130, Top = 210, Checked = Configuration.SMTPEnableSsl };

        Label lblDeliveryMethod = new Label() { Text = "Delivery Method:", Left = 10, Top = 240 };
        cmbDeliveryMethod = new ComboBox() { Left = 130, Top = 240, Width = 180 };
        cmbDeliveryMethod.Items.AddRange(Enum.GetNames(typeof(SmtpDeliveryMethod)));

        Label lblTimeout = new Label() { Text = "Timeout (ms):", Left = 10, Top = 270 };
        nudTimeout = new NumericUpDown() { Left = 130, Top = 270, Width = 80, Maximum = 60000, Value = Configuration.SMTPTimeout };

        btnSave = new Button() { Text = "Save", Left = 150, Top = 320 };

        btnSave.Click += onSave;

        Controls.AddRange(new Control[] { lblMaxPercentage, nudMaxPercentage, lblSMTP, txtSMTP, lblPort, txtPort,
            lblEmailSender, txtEmailSender, lblPassword, txtPassword, lblRecipient, txtRecipient, lblEnableSsl, chkEnableSsl,
            lblDeliveryMethod, cmbDeliveryMethod, lblTimeout, nudTimeout, btnSave });

        LoadConfiguration();
    }

    private void onSave(object? s, EventArgs e)
    {
        SetConfiguration();

        SaveConfiguration();

        if (SendTestEmail())
        {
            trayIcon.ShowBalloonTip(5000, "Success", "Configuration saved and test email sent successfully!", ToolTipIcon.Info);
            Close();
        }
        else
        {
            trayIcon.ShowBalloonTip(5000, "Error", "Failed to send test email. Please check the email configuration.", ToolTipIcon.Error);
        }
    }

    private bool SendTestEmail()
    {
        try
        {
            return emailService.QueueEmail("Test Email", "This is a test email to validate the configuration.");
        }
        catch (Exception ex)
        {
            trayIcon.ShowBalloonTip(5000, "Email Sending Error", $"Error sending test email: {ex.Message}", ToolTipIcon.Error);
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

                Configuration.MaxPercentageChange = double.Parse(configParts[0]);
                Configuration.SMTPServer = configParts[1];
                Configuration.SMTPPort = int.Parse(configParts[2]);
                Configuration.EmailSender = configParts[3];
                Configuration.EmailPassword = configParts[4];
                Configuration.EmailRecipient = configParts[5];
                Configuration.SMTPEnableSsl = bool.Parse(configParts[6]);
                Configuration.SMTPDeliveryMethod = Enum.Parse<SmtpDeliveryMethod>(configParts[7]);
                Configuration.SMTPTimeout = int.Parse(configParts[8]);

                GetConfiguration();

                trayIcon.ShowBalloonTip(5000, "Success", "Configuration loaded successfully!", ToolTipIcon.Info);
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
            var configData = $"{Configuration.MaxPercentageChange};{Configuration.SMTPServer};{Configuration.SMTPPort};" +
                             $"{Configuration.EmailSender};{Configuration.EmailPassword};{Configuration.EmailRecipient};" +
                             $"{Configuration.SMTPEnableSsl};{Configuration.SMTPDeliveryMethod};{Configuration.SMTPTimeout}";

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
        nudMaxPercentage.Value = (decimal)Configuration.MaxPercentageChange;
        txtSMTP.Text = Configuration.SMTPServer;
        txtPort.Text = Configuration.SMTPPort.ToString();
        txtEmailSender.Text = Configuration.EmailSender;
        txtPassword.Text = Configuration.EmailPassword;
        txtRecipient.Text = Configuration.EmailRecipient;
        chkEnableSsl.Checked = Configuration.SMTPEnableSsl;
        cmbDeliveryMethod.SelectedItem = Configuration.SMTPDeliveryMethod.ToString();
        nudTimeout.Value = Configuration.SMTPTimeout;
    }

    private void SetConfiguration()
    {
        Configuration.MaxPercentageChange = (double)nudMaxPercentage.Value;
        Configuration.SMTPServer = txtSMTP.Text;
        Configuration.SMTPPort = int.Parse(txtPort.Text);
        Configuration.EmailSender = txtEmailSender.Text;
        Configuration.EmailPassword = txtPassword.Text;
        Configuration.EmailRecipient = txtRecipient.Text;
        Configuration.SMTPEnableSsl = chkEnableSsl.Checked;
        Configuration.SMTPDeliveryMethod = Enum.Parse<SmtpDeliveryMethod>(cmbDeliveryMethod.SelectedItem?.ToString() ?? "Network");
        Configuration.SMTPTimeout = (int)nudTimeout.Value;
    }
}