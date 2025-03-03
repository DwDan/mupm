using System.Diagnostics;
using MUProcessMonitor.Forms;
using MUProcessMonitor.Models;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Context;

public class TrayApplicationContext : ApplicationContext
{
    private NotifyIcon trayIcon;
    private ProcessListForm processListForm;
    private Dictionary<int, long> initialMemoryUsage = new();
    private Dictionary<int, bool> monitoredProcesses = new();
    private Thread monitorThread;
    private bool isMonitoring = true;
    private TelegramService telegramService;

    public TrayApplicationContext()
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string resourcePath = Path.Combine(basePath, "Resources", "icon_mupm.ico");

        trayIcon = new NotifyIcon()
        {
            Icon = new Icon(resourcePath),
            Visible = true,
            Text = "Process Monitor"
        };

        trayIcon.ContextMenuStrip = new ContextMenuStrip();
        trayIcon.ContextMenuStrip.Items.Add("Open", null, OnOpen);
        trayIcon.ContextMenuStrip.Items.Add("Configure", null, OnConfigure);
        trayIcon.ContextMenuStrip.Items.Add("Exit", null, OnExit);

        trayIcon.DoubleClick += OnOpen;

        processListForm = new ProcessListForm(this);
        processListForm.Show();

        monitorThread = new Thread(MonitorProcesses) { IsBackground = true };
        monitorThread.Start();

        telegramService = new TelegramService(trayIcon);
    }

    public bool IsMonitoring(int processId)
    {
        return monitoredProcesses.ContainsKey(processId);
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        if (processListForm.IsDisposed)
        {
            processListForm = new ProcessListForm(this);
        }

        processListForm.Show();
        processListForm.WindowState = FormWindowState.Normal;
        processListForm.BringToFront();
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

    public void StartMonitoring(int processId)
    {
        if (!monitoredProcesses.ContainsKey(processId))
        {
            monitoredProcesses[processId] = true;
            try
            {
                var process = Process.GetProcessById(processId);
                initialMemoryUsage[processId] = process.WorkingSet64;
                trayIcon.ShowBalloonTip(3000, "Monitoring", $"Monitoring process {processId}.", ToolTipIcon.Info);
            }
            catch
            {
                monitoredProcesses.Remove(processId);
            }
        }
    }

    public void StopMonitoring(int processId)
    {
        if (monitoredProcesses.ContainsKey(processId))
        {
            monitoredProcesses.Remove(processId);
            initialMemoryUsage.Remove(processId);
            trayIcon.ShowBalloonTip(3000, "Monitoring", $"Monitoring stopped for process {processId}.", ToolTipIcon.Info);
        }
    }

    private void MonitorProcesses()
    {
        while (isMonitoring)
        {
            Thread.Sleep(2000);
            var processesToRemove = new List<int>();

            foreach (var processId in monitoredProcesses.Keys.ToList())
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    long currentMemoryUsage = process.WorkingSet64;

                    if (initialMemoryUsage.TryGetValue(processId, out long previousMemoryUsage))
                    {
                        long difference = Math.Abs(currentMemoryUsage - previousMemoryUsage);
                        double percentageChange = (double)difference / previousMemoryUsage * 100;

                        if (percentageChange > Configuration.MaxPercentageChange)
                        {
                            string message = $"Process {processId} increased memory usage by {percentageChange:F2}%!";
                            trayIcon.ShowBalloonTip(5000, "Memory Alert", message, ToolTipIcon.Warning);
                            telegramService.QueueNotification("Memory Alert", message);

                        }
                    }

                    initialMemoryUsage[processId] = currentMemoryUsage;
                }
                catch
                {
                    processesToRemove.Add(processId);
                    string message = $"Process {processId} has been terminated.";
                    trayIcon.ShowBalloonTip(5000, "Process Terminated", message, ToolTipIcon.Warning);
                    telegramService.QueueNotification("Process Terminated", message);
                }
            }

            foreach (var processId in processesToRemove)
            {
                StopMonitoring(processId);
            }
        }
    }
}
