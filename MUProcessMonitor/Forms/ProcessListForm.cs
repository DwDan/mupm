using System.Diagnostics;
using MUProcessMonitor.Context;

namespace MUProcessMonitor.Forms;

public class ProcessListForm : Form
{
    private ListView listView;
    private Button btnRefresh, btnConfigure, btnMinimize;
    private TrayApplicationContext trayContext;
    private ContextMenuStrip contextMenuStrip;

    public ProcessListForm(TrayApplicationContext context)
    {
        trayContext = context;
        Text = "MU Process Monitor";
        Width = 500;
        Height = 400;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += trayContext.OnExit;
        Resize += ProcessListForm_Resize;

        listView = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Width = 480,
            Height = 300,
            Left = 10,
            Top = 10
        };

        listView.Columns.Add("PID", 50);
        listView.Columns.Add("Name", 150);
        listView.Columns.Add("Memory", 100);
        listView.Columns.Add("Status", 100);

        listView.MouseClick += ListView_MouseClick;

        contextMenuStrip = new ContextMenuStrip();

        btnRefresh = new Button { Text = "Refresh", Left = 100, Width = 100, Top = 320 };
        btnRefresh.Click += (s, e) => LoadProcesses();

        btnConfigure = new Button { Text = "Configure", Left = 210, Width = 100, Top = 320 };
        btnConfigure.Click += (s, e) => trayContext.OnConfigure(s, e);

        btnMinimize = new Button { Text = "Minimize", Left = 320, Width = 100, Top = 320 };
        btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;

        Controls.Add(listView);
        Controls.Add(btnRefresh);
        Controls.Add(btnConfigure);
        Controls.Add(btnMinimize);

        LoadProcesses();
    }

    private void LoadProcesses()
    {
        listView.Items.Clear();
        var processes = Process.GetProcesses().Where(p => p.ProcessName.Equals("main", StringComparison.OrdinalIgnoreCase));

        foreach (var proc in processes)
        {
            var status = trayContext.IsMonitoring(proc.Id) ? "Monitoring" : "Not Monitoring";

            var item = new ListViewItem(proc.Id.ToString());
            item.SubItems.Add(proc.ProcessName);
            item.SubItems.Add($"{proc.PrivateMemorySize64 / 1024 / 1024} MB");
            item.SubItems.Add(status);

            listView.Items.Add(item);
        }
    }

    private void ListView_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && listView.FocusedItem != null && listView.FocusedItem.Bounds.Contains(e.Location))
        {
            contextMenuStrip.Items.Clear();

            int processId = int.Parse(listView.FocusedItem.Text);
            bool isMonitoring = trayContext.IsMonitoring(processId);

            if (!isMonitoring)
            {
                var startItem = new ToolStripMenuItem("Start Monitoring");
                startItem.Click += (s, ev) => StartMonitoring(processId);
                contextMenuStrip.Items.Add(startItem);
            }
            else
            {
                var stopItem = new ToolStripMenuItem("Stop Monitoring");
                stopItem.Click += (s, ev) => StopMonitoring(processId);
                contextMenuStrip.Items.Add(stopItem);
            }

            contextMenuStrip.Show(Cursor.Position);
        }
    }

    private void StartMonitoring(int processId)
    {
        trayContext.StartMonitoring(processId);
        LoadProcesses();
    }

    private void StopMonitoring(int processId)
    {
        trayContext.StopMonitoring(processId);
        LoadProcesses();
    }

    private void ProcessListForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
    }
}
