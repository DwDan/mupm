﻿using System.Runtime.InteropServices;
using System.Text;
using MUProcessMonitor.Models;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Manager;

public class WindowMonitorManager
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    private WindowMonitorService _windowMonitorService;
    private HelperMonitorService _helperMonitorService;
    private NotificationManager _notificationManager;
    private AlarmManager _alarmManager;
    private ScreenShotManager _screenShotManager;

    private Thread monitorThread;
    private ManualResetEvent stopMonitorEvent = new(false);
    private bool isMonitoring = true;
    public event EventHandler? MonitoringUpdated;

    private static readonly Lazy<WindowMonitorManager> _instance = new(() => new WindowMonitorManager());
    public static WindowMonitorManager Instance => _instance.Value;

    public WindowMonitorManager()
    {
        monitorThread = new Thread(MonitorWindows) { IsBackground = true };
        monitorThread.Start();

        _windowMonitorService = new WindowMonitorService();
        _helperMonitorService = new HelperMonitorService();
        _notificationManager = NotificationManager.Instance;
        _alarmManager = AlarmManager.Instance;
        _screenShotManager = ScreenShotManager.Instance;
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
    
    public void StopMonitoring()
    {
        isMonitoring = false;
        stopMonitorEvent.Set();
        monitorThread.Join();
    }

    public bool IsMonitoring(int windowHandle)
    {
        return _windowMonitorService.IsMonitoring(windowHandle);
    }

    public List<ListViewItem> LoadWindows()
    {
        var listView = new List<ListViewItem>();

        _screenShotManager.Clear();

        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                var windowText = new StringBuilder(256);
                GetWindowText(hWnd, windowText, windowText.Capacity);

                if (windowText.ToString().Equals("MU", StringComparison.OrdinalIgnoreCase))
                {
                    var status = IsMonitoring((int)hWnd) ? "Monitoring" : "Not Monitoring";
                    var windowRect = _helperMonitorService.GetWindowRectangle((int)hWnd);
                    var screenshot = _helperMonitorService.CaptureScreen(windowRect);

                    var item = new ListViewItem(hWnd.ToString());
                    item.SubItems.Add(windowText.ToString());
                    item.SubItems.Add(status);

                    if (screenshot != null)
                        _screenShotManager.AddScreenshot(hWnd.ToString(), screenshot);

                    listView.Add(item);
                }
            }
            return true;
        }, IntPtr.Zero);

        return listView;
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

    private void OnMonitoringUpdated()
    {
        MonitoringUpdated?.Invoke(this, EventArgs.Empty);
    }
}
