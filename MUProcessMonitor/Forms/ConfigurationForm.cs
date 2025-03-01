using MUProcessMonitor.Models;

namespace MUProcessMonitor.Forms;

public class ConfigurationForm : Form
{
    private NumericUpDown nudMaxPercentage;
    private TextBox txtSMTP, txtPort, txtEmailSender, txtPassword, txtRecipient;
    private Button btnSave;

    public ConfigurationForm()
    {
        Text = "Configuration";
        Width = 350;
        Height = 300;

        Label lblMaxPercentage = new Label() { Text = "Max % Change:", Left = 10, Top = 20 };
        nudMaxPercentage = new NumericUpDown() { Left = 130, Top = 20, Width = 50, Value = (decimal)Configuration.MaxPercentageChange };

        Label lblSMTP = new Label() { Text = "SMTP Server:", Left = 10, Top = 60 };
        txtSMTP = new TextBox() { Left = 130, Top = 60, Width = 180, Text = Configuration.SMTPServer };

        Label lblPort = new Label() { Text = "Port:", Left = 10, Top = 90 };
        txtPort = new TextBox() { Left = 130, Top = 90, Width = 50, Text = Configuration.SMTPPort.ToString() };

        Label lblEmailSender = new Label() { Text = "Sender Email:", Left = 10, Top = 120 };
        txtEmailSender = new TextBox() { Left = 130, Top = 120, Width = 180, Text = Configuration.EmailSender };

        Label lblPassword = new Label() { Text = "Password:", Left = 10, Top = 150 };
        txtPassword = new TextBox() { Left = 130, Top = 150, Width = 180, Text = Configuration.EmailPassword, PasswordChar = '*' };

        Label lblRecipient = new Label() { Text = "Recipient:", Left = 10, Top = 180 };
        txtRecipient = new TextBox() { Left = 130, Top = 180, Width = 180, Text = Configuration.EmailRecipient };

        btnSave = new Button() { Text = "Save", Left = 120, Top = 220 };
        btnSave.Click += (s, e) =>
        {
            Configuration.MaxPercentageChange = (double)nudMaxPercentage.Value;
            Configuration.SMTPServer = txtSMTP.Text;
            Configuration.SMTPPort = int.Parse(txtPort.Text);
            Configuration.EmailSender = txtEmailSender.Text;
            Configuration.EmailPassword = txtPassword.Text;
            Configuration.EmailRecipient = txtRecipient.Text;
            Close();
        };

        Controls.AddRange(new Control[] { lblMaxPercentage, nudMaxPercentage, lblSMTP, txtSMTP, lblPort, txtPort,
            lblEmailSender, txtEmailSender, lblPassword, txtPassword, lblRecipient, txtRecipient, btnSave });
    }
}
