using MUProcessMonitor.Forms;
using MUProcessMonitor.Manager;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Context;

public class TrayApplicationContextWindows : ApplicationContext
{
    public NotificationManager NotificationManager;
    public AlarmManager AlarmManager;
    public ConfigurationManager ConfigurationManager;

    private NotifyIcon _trayIcon;
    private MUWindowListForm _windowListForm;
    private WindowMonitorService _windowMonitorService;

    public TrayApplicationContextWindows()
    {
        InitializeTrayIcon();

        NotificationManager = new NotificationManager(_trayIcon ?? throw new ArgumentNullException(nameof(_trayIcon)));
        AlarmManager = new AlarmManager();
        ConfigurationManager = new ConfigurationManager();
        ConfigurationManager.LoadConfiguration();

        _windowMonitorService = new WindowMonitorService(this);

        _windowListForm = new MUWindowListForm(this);
        _windowListForm.Show();
    }

    private void InitializeTrayIcon()
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string resourcePath = Path.Combine(basePath, "Resources", "icon_mupm.ico");

        _trayIcon = new NotifyIcon
        {
            Icon = new Icon(resourcePath),
            Visible = true,
            Text = "Window Monitor",
            ContextMenuStrip = new ContextMenuStrip()
        };

        _trayIcon.ContextMenuStrip.Items.Add("Open", null, OnOpen);
        _trayIcon.ContextMenuStrip.Items.Add("Configure", null, OnConfigure);
        _trayIcon.ContextMenuStrip.Items.Add("Stop Alarm", null, StopAlarm);
        _trayIcon.ContextMenuStrip.Items.Add("Exit", null, OnExit);

        _trayIcon.DoubleClick += OnOpen;
    }

    public void OnOpen(object? sender, EventArgs e)
    {
        if (_windowListForm.IsDisposed)
        {
            _windowListForm = new MUWindowListForm(this);
        }

        _windowListForm.Show();
        _windowListForm.WindowState = FormWindowState.Normal;
        _windowListForm.BringToFront();
    }

    public void OnConfigure(object? sender, EventArgs e)
    {
        using ConfigurationTelegramForm form = new ConfigurationTelegramForm(this);
        form.ShowDialog();
    }

    public void OnExit(object? sender, EventArgs e)
    {
        AlarmManager.StopAlarm();
        _windowMonitorService.StopMonitoring();
        _trayIcon.Visible = false;
        Application.ExitThread();
        Application.Exit();
    }

    public void StopAlarm(object? sender, EventArgs e)
        => AlarmManager.StopAlarm();

    public bool IsMonitoring(int windowHandle)
        => _windowMonitorService.IsMonitoring(windowHandle);

    public void StartMonitoring(int windowHandle)
    {
        _windowMonitorService.StartMonitoring(windowHandle);

        NotificationManager.ShowBalloonTip("Monitoring started", $"Monitoring started for window with handle {windowHandle}", ToolTipIcon.Info);
    }

    public void StopMonitoring(int windowHandle)
    {
        _windowMonitorService.StopMonitoring(windowHandle);
        _windowListForm.SafeLoadWindows();

        NotificationManager.ShowBalloonTip("Monitoring stopped", $"Monitoring stopped for window with handle {windowHandle}", ToolTipIcon.Warning);
    }
}
