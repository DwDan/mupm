using MUProcessMonitor.Forms;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Context;

public class TrayApplicationContextWindows : ApplicationContext
{
    private NotifyIcon trayIcon;
    private MUWindowListForm windowListForm;
    private Dictionary<int, bool> monitoredWindows = new();
    private Thread monitorThread;
    private TelegramService telegramService;
    private HelperMonitorService helperMonitorService;
    private bool isMonitoring = true;

    public TrayApplicationContextWindows()
    {
        trayIcon = new NotifyIcon()
        {
            Icon = SystemIcons.Asterisk,
            Visible = true,
            Text = "Window Monitor"
        };

        trayIcon.ContextMenuStrip = new ContextMenuStrip();
        trayIcon.ContextMenuStrip.Items.Add("Open", null, OnOpen);
        trayIcon.ContextMenuStrip.Items.Add("Configure", null, OnConfigure);
        trayIcon.ContextMenuStrip.Items.Add("Exit", null, OnExit);

        trayIcon.DoubleClick += OnOpen;

        windowListForm = new MUWindowListForm(this);
        windowListForm.Show();

        monitorThread = new Thread(MonitorWindows) { IsBackground = true };
        monitorThread.Start();

        telegramService = new TelegramService(trayIcon);
        helperMonitorService = new HelperMonitorService();
    }

    public bool IsMonitoring(int windowHandle)
    {
        return monitoredWindows.ContainsKey(windowHandle);
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        if (windowListForm.IsDisposed)
        {
            windowListForm = new MUWindowListForm(this);
        }

        windowListForm.Show();
        windowListForm.WindowState = FormWindowState.Normal;
        windowListForm.BringToFront();
    }

    public void OnConfigure(object? sender, EventArgs e)
    {
        using (var form = new ConfigurationTelegramForm(trayIcon))
        {
            form.ShowDialog();
        }
    }

    public void OnExit(object? sender, EventArgs e)
    {
        isMonitoring = false;
        monitorThread.Join();

        trayIcon.Visible = false;

        Application.ExitThread();
        Application.Exit();
    }

    public void StartMonitoring(int windowHandle)
    {
        if (!monitoredWindows.ContainsKey(windowHandle))
        {
            monitoredWindows[windowHandle] = true;
            trayIcon.ShowBalloonTip(3000, "Monitoring", $"Monitoring window {windowHandle}.", ToolTipIcon.Info);
        }
    }

    public void StopMonitoring(int windowHandle)
    {
        if (monitoredWindows.ContainsKey(windowHandle))
        {
            monitoredWindows.Remove(windowHandle);
            trayIcon.ShowBalloonTip(3000, "Monitoring", $"Monitoring stopped for window {windowHandle}.", ToolTipIcon.Info);
        }
    }

    private void MonitorWindows()
    {
        while (isMonitoring)
        {
            Thread.Sleep(5000);

            foreach (var windowHandle in monitoredWindows.Keys.ToList())
            {
                try
                {
                    var windowRect = helperMonitorService.GetWindowRectangle(windowHandle);
                    if (helperMonitorService.IsHelperInactive(windowHandle, windowRect))
                    {
                        string message = $"Helper inactive on window {windowHandle}.";
                        trayIcon.ShowBalloonTip(5000, "Helper Inactive", message, ToolTipIcon.Warning);
                        telegramService.QueueNotification("Helper Inactive", message);
                        StopMonitoring(windowHandle);
                    }
                }
                catch
                {
                    StopMonitoring(windowHandle);
                    string message = $"Window {windowHandle} has been closed.";
                    trayIcon.ShowBalloonTip(5000, "Window Closed", message, ToolTipIcon.Warning);
                    telegramService.QueueNotification("Window Closed", message);
                }
            }
        }
    }
}
