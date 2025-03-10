using System.Text;
using MUProcessMonitor.Models;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Manager;

public class WindowMonitorManager
{
    private static readonly Lazy<WindowMonitorManager> _instance = new(() => new WindowMonitorManager());
    public static WindowMonitorManager Instance => _instance.Value;

    private WindowMonitorService _windowMonitorService;
    private HelperMonitorService _helperMonitorService;
    private NotificationManager _notificationManager;
    private AlarmManager _alarmManager;
    private ScreenShotManager _screenShotManager;

    private Thread monitorThread;
    private Thread marketingThread;
    private ManualResetEvent stopMonitorEvent = new(false);
    private bool isMonitoring = true;
    private bool isMarketing = true;
    public event EventHandler? MonitoringUpdated;

    public WindowMonitorManager()
    {
        _windowMonitorService = new WindowMonitorService();
        _helperMonitorService = new HelperMonitorService();
        _notificationManager = NotificationManager.Instance;
        _alarmManager = AlarmManager.Instance;
        _screenShotManager = ScreenShotManager.Instance;

        monitorThread = new Thread(MonitorWindows) { IsBackground = true };
        monitorThread.Start();

        marketingThread = new Thread(MonitorMarketing) { IsBackground = true };
        marketingThread.Start();
    }

    public void StartMonitoring(int windowHandle)
    {
        _windowMonitorService.StartMonitoring(windowHandle);

        OnMonitoringUpdated();
    }

    public void StopMonitoring(int windowHandle)
    {
        _windowMonitorService.StopMonitoring(windowHandle);

        OnMonitoringUpdated();
    }

    public void StartMarketing(int windowHandle)
    {
        _windowMonitorService.StartMarketing(windowHandle);

        OnMonitoringUpdated();
    }

    public void StopMarketing(int windowHandle)
    {
        _windowMonitorService.StopMarketing(windowHandle);

        OnMonitoringUpdated();
    }

    public void StopMonitoring()
    {
        isMonitoring = false;
        isMarketing = false;
        stopMonitorEvent.Set();
        monitorThread.Join();
    }

    public bool IsMonitoring(int windowHandle)
    {
        return _windowMonitorService.IsMonitoring(windowHandle);
    }

    public bool IsMarketing(int windowHandle)
    {
        return _windowMonitorService.IsMarketing(windowHandle);
    }

    public List<ListViewItem> LoadWindows()
    {
        var listView = new List<ListViewItem>();

        _screenShotManager.Clear();

        WindowMonitorService.EnumWindows((hWnd, lParam) =>
        {
            if (WindowMonitorService.IsWindowVisible(hWnd))
            {
                var windowText = new StringBuilder(256);
                WindowMonitorService.GetWindowText(hWnd, windowText, windowText.Capacity);

                if (windowText.ToString().Equals("MU", StringComparison.OrdinalIgnoreCase))
                {
                    var status = IsMarketing((int)hWnd) ? "Marketing" :
                        IsMonitoring((int)hWnd) ? "Monitoring" :
                        "Not Monitoring";

                    var windowRect = _helperMonitorService.GetWindowRectangle((int)hWnd);
                    var screenshot = _helperMonitorService.CaptureScreen(windowRect);

                    var item = new ListViewItem(hWnd.ToString());
                    item.SubItems.Add(windowText.ToString());
                    item.SubItems.Add(status);

                    if (screenshot != null)
                        _screenShotManager.AddScreenshot((int)hWnd, screenshot);

                    listView.Add(item);
                }
            }
            return true;
        }, IntPtr.Zero);

        return listView.OrderBy(item => int.Parse(item.Text)).ToList();
    }

    private void MonitorWindows()
    {
        while (isMonitoring)
        {
            if (stopMonitorEvent.WaitOne(Configuration.ThreadSleepTime))
                break;

            foreach (var windowHandle in _windowMonitorService.GetMonitoredWindowHandles())
            {
                try
                {
                    var windowRect = _helperMonitorService.GetWindowRectangle(windowHandle);
                    if (_helperMonitorService.IsHelperInactive(windowHandle, windowRect))
                    {
                        string message = $"Helper inactive on window {windowHandle}.";
                        _windowMonitorService.StopMonitoring(windowHandle);
                        _notificationManager.SendNotification("Helper Inactive", message, ToolTipIcon.Warning);
                        _alarmManager.StartAlarm();
                        OnMonitoringUpdated();
                    }
                }
                catch
                {
                    string message = $"Window {windowHandle} has been closed.";
                    _windowMonitorService.StopMonitoring(windowHandle);
                    _notificationManager.SendNotification("Window Closed", message, ToolTipIcon.Warning);
                    _alarmManager.StartAlarm();
                    OnMonitoringUpdated();
                }
            }
        }
    }    
    
    private void MonitorMarketing()
    {
        while (isMarketing)
        {
            if (stopMonitorEvent.WaitOne(Configuration.ThreadSleepTime))
                break;

            foreach (var windowHandle in _windowMonitorService.GetMarketedWindowHandles())
            {
                try
                {
                    var windowRect = _helperMonitorService.GetWindowRectangle(windowHandle);
                    if (_helperMonitorService.IsMessageReceived(windowHandle, windowRect))
                    {
                        string message = $"Message received on window {windowHandle}.";
                        _windowMonitorService.StopMarketing(windowHandle);
                        _notificationManager.SendNotification("Message received", message, ToolTipIcon.Info);
                        _alarmManager.PlaySelectedSound("alert_10.mp3");
                        OnMonitoringUpdated();
                    }
                }
                catch
                {
                    string message = $"Window {windowHandle} has been closed.";
                    _windowMonitorService.StopMarketing(windowHandle);
                    _notificationManager.SendNotification("Window Closed", message, ToolTipIcon.Warning);
                    _alarmManager.StartAlarm();
                    OnMonitoringUpdated();
                }
            }
        }
    }

    private void OnMonitoringUpdated()
    {
        MonitoringUpdated?.Invoke(this, EventArgs.Empty);
    }
}
