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
        Width = 450;
        Height = 500;
        StartPosition = FormStartPosition.CenterScreen;

        _gamePreviewPictureBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Normal,
            Width = 400,
            Height = 400,
            Location = new Point(25, 25)
        };

        _calculateNextStepButton = new Button
        {
            Text = "Calculate Next Step",
            Dock = DockStyle.Bottom,
            Height = 40,
        };

        _calculateNextStepButton.Click += (s, e) => _gamePreviewPictureBox.Image = _findBombManager.CalculateNextStep();

        Controls.Add(_gamePreviewPictureBox);
        Controls.Add(_calculateNextStepButton);

        _gamePreviewPictureBox.Image = _findBombManager.LoadGameCapture(hWnd);
    }
}