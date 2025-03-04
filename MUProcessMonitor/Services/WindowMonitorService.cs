namespace MUProcessMonitor.Services;

public class WindowMonitorService
{
    private Dictionary<int, bool> monitoredWindows = new();

    public List<int> GetMonitoredWindowHandles()
    {
        return monitoredWindows.Keys.ToList();
    }

    public bool IsMonitoring(int windowHandle)
    {
        return monitoredWindows.ContainsKey(windowHandle);
    }

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
}

