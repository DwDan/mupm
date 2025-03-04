using MUProcessMonitor.Manager;

namespace MUProcessMonitor.Forms;

public class MUWindowListForm : Form
{
    private readonly ListView _listView;
    private readonly ContextMenuStrip _contextMenuStrip;

    private readonly NotificationManager _notificationManager;
    private readonly AlarmManager _alarmManager;
    private readonly ConfigurationManager _configurationManager;
    private readonly WindowMonitorManager _windowMonitorManager;
    private readonly ScreenShotManager _screenShotManager;

    public MUWindowListForm()
    {
        _alarmManager = AlarmManager.Instance;
        _configurationManager = ConfigurationManager.Instance;
        _screenShotManager = ScreenShotManager.Instance;
        _notificationManager = NotificationManager.Instance;
        _windowMonitorManager = WindowMonitorManager.Instance;
        _windowMonitorManager.MonitoringUpdated += SafeLoadWindows;
        _configurationManager.LoadConfiguration();

        Text = "MU Window Monitor";
        Width = 500;
        Height = 400;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += OnExit;
        Resize += WindowListForm_Resize;

        _listView = InitializeListView();
        _contextMenuStrip = new ContextMenuStrip();

        Controls.Add(_listView);
        Controls.Add(CreateButton("Refresh", 100, 320, (s, e) => LoadWindows()));
        Controls.Add(CreateButton("Configure", 210, 320, OnConfigure));
        Controls.Add(CreateButton("Stop Alarm", 320, 320, StopAlarm));

        LoadWindows();
    }

    private ListView InitializeListView()
    {
        var listView = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Width = 465,
            Height = 300,
            Left = 10,
            Top = 10
        };

        listView.Columns.Add("Handle", 100);
        listView.Columns.Add("Title", 150);
        listView.Columns.Add("Status", 100);
        listView.MouseClick += ListView_MouseClick;

        return listView;
    }

    private Button CreateButton(string text, int left, int top, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Left = left,
            Width = 100,
            Top = top
        };
        button.Click += onClick;
        return button;
    }

    private void LoadWindows()
    {
        _listView.Items.Clear();
        _listView.Items.AddRange(_windowMonitorManager.LoadWindows().ToArray());
    }

    private void ListView_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && _listView.FocusedItem != null && _listView.FocusedItem.Bounds.Contains(e.Location))
        {
            _contextMenuStrip.Items.Clear();
            int handle = int.Parse(_listView.FocusedItem.Text);

            _contextMenuStrip.Items.Add(IsMonitoring(handle)
                ? CreateMenuItem("Stop Monitoring", () => StopMonitoring(handle))
                : CreateMenuItem("Start Monitoring", () => StartMonitoring(handle)));

            if (_screenShotManager.ContainsScreenshot(handle.ToString()))
                _contextMenuStrip.Items.Add(CreateMenuItem("View Screenshot", () => _screenShotManager.ShowScreenshot(handle.ToString())));

            _contextMenuStrip.Show(Cursor.Position);
        }
    }

    private ToolStripMenuItem CreateMenuItem(string text, Action onClick)
    {
        var menuItem = new ToolStripMenuItem(text);
        menuItem.Click += (s, e) => onClick();
        return menuItem;
    }

    private void StartMonitoring(int windowHandle)
    {
        _windowMonitorManager.StartMonitoring(windowHandle);
        _notificationManager.ShowBalloonTip("Monitoring started", $"Monitoring started for window {windowHandle}");
    }

    private void StopMonitoring(int windowHandle)
    {
        _windowMonitorManager.StopMonitoring(windowHandle);
        _notificationManager.ShowBalloonTip("Monitoring stopped", $"Monitoring stopped for window {windowHandle}", ToolTipIcon.Warning);
    }

    private void SafeLoadWindows(object? sender, EventArgs e)
    {
        if (InvokeRequired)
            Invoke(new Action(LoadWindows));
        else
            LoadWindows();
    }

    private void WindowListForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized) Hide();
    }

    public void OnConfigure(object? sender, EventArgs e)
    {
        using var form = new ConfigurationTelegramForm();
        form.ShowDialog();
    }

    public void OnExit(object? sender, EventArgs e)
    {
        _alarmManager.StopAlarm();
        _windowMonitorManager.StopMonitoring();
        TrayIconManager.Instance.Visible = false;
        Application.ExitThread();
        Application.Exit();
    }

    public void StopAlarm(object? sender, EventArgs e)
    {
        _alarmManager.StopAlarm();
    }

    public bool IsMonitoring(int windowHandle)
    {
        return _windowMonitorManager.IsMonitoring(windowHandle);
    }
}
