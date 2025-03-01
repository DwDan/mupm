using System.Diagnostics;
using MUProcessMonitor.Context;

namespace MUProcessMonitor.Forms;

public class ProcessListForm : Form
{
    private ListView listView;
    private Button btnRefresh, btnStartMonitor, btnStopMonitor;
    private TrayApplicationContext trayContext;

    public ProcessListForm(TrayApplicationContext context)
    {
        trayContext = context;
        Text = "MU Process Monitor";
        Width = 400;
        Height = 300;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += ProcessListForm_FormClosing;

        listView = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Width = 360,
            Height = 200,
            Left = 10,
            Top = 10
        };
        listView.Columns.Add("PID", 50);
        listView.Columns.Add("Name", 150);
        listView.Columns.Add("Memory", 100);
        LoadProcesses();

        btnRefresh = new Button { Text = "Refresh", Left = 10, Width = 100, Top = 220 };
        btnRefresh.Click += (s, e) => LoadProcesses();

        btnStartMonitor = new Button { Text = "Start", Left = 120, Width = 80, Top = 220 };
        btnStartMonitor.Click += (s, e) => StartMonitoring();

        btnStopMonitor = new Button { Text = "Stop", Left = 210, Width = 80, Top = 220 };
        btnStopMonitor.Click += (s, e) => StopMonitoring();

        Controls.Add(listView);
        Controls.Add(btnRefresh);
        Controls.Add(btnStartMonitor);
        Controls.Add(btnStopMonitor);
    }

    private void LoadProcesses()
    {
        listView.Items.Clear();

        var processes = Process.GetProcesses().Where(p => p.ProcessName.Equals("main", StringComparison.OrdinalIgnoreCase));

        foreach (var proc in processes)
        {
            var item = new ListViewItem(proc.Id.ToString());
            item.SubItems.Add(proc.ProcessName);
            item.SubItems.Add($"{proc.PrivateMemorySize64 / 1024 / 1024} MB");
            listView.Items.Add(item);
        }
    }

    private void StartMonitoring()
    {
        if (listView.SelectedItems.Count > 0)
        {
            int processId = int.Parse(listView.SelectedItems[0].Text);
            trayContext.StartMonitoring(processId);
        }
    }

    private void StopMonitoring()
    {
        if (listView.SelectedItems.Count > 0)
        {
            int processId = int.Parse(listView.SelectedItems[0].Text);
            trayContext.StopMonitoring(processId);
        }
    }

    private void ProcessListForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
