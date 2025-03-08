using System.Runtime.InteropServices;
using System.Text;

namespace MUProcessMonitor.Services;

public class WindowMonitorService
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool IsWindowVisible(IntPtr hWnd);


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