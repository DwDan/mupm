using MUProcessMonitor.Helpers;
using MUProcessMonitor.Manager;

namespace MUProcessMonitor.Forms;

public class FindBombForm : Form
{
    private readonly FindBombManager _findBombManager;
    private readonly PictureBox _gamePreviewPictureBox;
    private readonly Button _calculateNextStepButton;

    public FindBombForm(int hWnd)
    {
        _findBombManager = FindBombManager.Instance;

        Icon = BitmapHelper.LoadIcon("icon_mupm.ico");
        Text = "Find Bombs";
        Width = 260;
        Height = 320;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;

        _gamePreviewPictureBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Normal,
            Width = 200,
            Height = 200,
            Location = new Point(25, 25)
        };

        _calculateNextStepButton = new Button
        {
            Text = "Calculate Next Step",
            Height = 30,
            Width = 150,
            Left = 50,
            Top = 230
        };

        _calculateNextStepButton.Click += (s, e) => _gamePreviewPictureBox.Image = _findBombManager.CalculateNextStep();

        Controls.Add(_gamePreviewPictureBox);
        Controls.Add(_calculateNextStepButton);

        _gamePreviewPictureBox.Image = _findBombManager.LoadGameCapture(hWnd);
    }
}