using MUProcessMonitor.Forms;
using MUProcessMonitor.Manager;

namespace MUProcessMonitor.Context;

public class TrayApplicationContextWindows : ApplicationContext
{
    private MUWindowListForm _windowListForm;

    public TrayApplicationContextWindows()
    {
        _windowListForm = new MUWindowListForm();

        InitializeTrayIcon();

        _windowListForm.Show();
    }

    private void InitializeTrayIcon()
    {
        TrayIconManager.ClearContextMenu();

        TrayIconManager.AddContextMenuItem("Open", OnOpen);
        TrayIconManager.AddContextMenuItem("Configure", _windowListForm.OnConfigure);
        TrayIconManager.AddContextMenuItem("Stop Alarm", _windowListForm.StopAlarm);
        TrayIconManager.AddContextMenuItem("Exit", _windowListForm.OnExit);

        TrayIconManager.Instance.DoubleClick += OnOpen;
    }

    public void OnOpen(object? sender, EventArgs e)
    {
        if (_windowListForm.IsDisposed)
            _windowListForm = new MUWindowListForm();

        _windowListForm.Show();
        _windowListForm.WindowState = FormWindowState.Normal;
        _windowListForm.BringToFront();
    }
}
