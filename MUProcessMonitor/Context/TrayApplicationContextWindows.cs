using MUProcessMonitor.Forms;
using MUProcessMonitor.Models;
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
    private AlarmService alarmService;
    private bool isMonitoring = true;
    private bool isAlarmPlaying = false;

    public TrayApplicationContextWindows()
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string resourcePath = Path.Combine(basePath, "Resources", "icon_mupm.ico");
        string alarmPath = Path.Combine(basePath, "Resources", "alarm.wav");

        trayIcon = new NotifyIcon()
        {
            Icon = new Icon(resourcePath),
            Visible = true,
            Text = "Window Monitor"
        };

        trayIcon.ContextMenuStrip = new ContextMenuStrip();
        trayIcon.ContextMenuStrip.Items.Add("Open", null, OnOpen);
        trayIcon.ContextMenuStrip.Items.Add("Configure", null, OnConfigure);
        trayIcon.ContextMenuStrip.Items.Add("Stop Alarm", null, StopAlarm);
        trayIcon.ContextMenuStrip.Items.Add("Exit", null, OnExit);

        trayIcon.DoubleClick += OnOpen;

        windowListForm = new MUWindowListForm(this);
        windowListForm.Show();

        monitorThread = new Thread(MonitorWindows) { IsBackground = true };
        monitorThread.Start();

        telegramService = new TelegramService(trayIcon);
        helperMonitorService = new HelperMonitorService();
        alarmService = new AlarmService();
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
        isAlarmPlaying = false;
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
            windowListForm.SafeLoadWindows();
        }
    }

    private void MonitorWindows()
    {
        while (isMonitoring)
        {
            Thread.Sleep(Configuration.ThreadSleepTime);

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

                        if (Configuration.UseAlarm && !isAlarmPlaying)
                            StartAlarm();
                    }
                }
                catch
                {
                    StopMonitoring(windowHandle);
                    string message = $"Window {windowHandle} has been closed.";
                    trayIcon.ShowBalloonTip(5000, "Window Closed", message, ToolTipIcon.Warning);
                    telegramService.QueueNotification("Window Closed", message);

                    if (Configuration.UseAlarm && !isAlarmPlaying)
                        StartAlarm();
                }
            }
        }
    }

    private void StartAlarm()
    {
        isAlarmPlaying = true;
        new Thread(() =>
        {
            while (isAlarmPlaying)
            {
                alarmService.Start().Wait();
            }
        })
        { IsBackground = true }.Start();
    }

    public void StopAlarm(object? sender, EventArgs e)
    {
        isAlarmPlaying = false;
        alarmService.Stop();
    }
}
