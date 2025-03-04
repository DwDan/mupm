using System.Windows.Forms;
using global::MUProcessMonitor.Context;
using MUProcessMonitor.Models;

namespace MUProcessMonitor.Services;

public class WindowMonitorService
{
    private Dictionary<int, bool> monitoredWindows = new();
    private HelperMonitorService helperMonitorService = new();
    private ManualResetEvent stopMonitorEvent = new(false);
    private Thread monitorThread;
    private TrayApplicationContextWindows appContext;
    private bool isMonitoring = true;

    public WindowMonitorService(TrayApplicationContextWindows context)
    {
        appContext = context;
        monitorThread = new Thread(MonitorWindows) { IsBackground = true };
        monitorThread.Start();
    }

    public bool IsMonitoring(int windowHandle) => monitoredWindows.ContainsKey(windowHandle);

    public void StartMonitoring(int windowHandle)
    {
        if (!IsMonitoring(windowHandle))
            monitoredWindows[windowHandle] = true;
    }

    public void StopMonitoring(int windowHandle)
    {
        if (IsMonitoring(windowHandle))
            monitoredWindows.Remove(windowHandle);
    }

    public void StopMonitoring()
    {
        isMonitoring = false;
        stopMonitorEvent.Set();
        monitorThread.Join();
    }

    private void MonitorWindows()
    {
        while (isMonitoring)
        {
            if (stopMonitorEvent.WaitOne(Configuration.ThreadSleepTime))
                break;

            foreach (var windowHandle in monitoredWindows.Keys.ToList())
            {
                try
                {
                    var windowRect = helperMonitorService.GetWindowRectangle(windowHandle);
                    if (helperMonitorService.IsHelperInactive(windowHandle, windowRect))
                    {
                        string message = $"Helper inactive on window {windowHandle}.";
                        appContext.StopMonitoring(windowHandle);
                        appContext.NotificationManager.SendNotification("Helper Inactive", message, ToolTipIcon.Warning);
                        appContext.AlarmManager.StartAlarm();
                    }
                }
                catch
                {
                    string message = $"Window {windowHandle} has been closed.";
                    appContext.StopMonitoring(windowHandle);
                    appContext.NotificationManager.SendNotification("Window Closed", message, ToolTipIcon.Warning);
                    appContext.AlarmManager.StartAlarm();
                }
            }
        }
    }
}

