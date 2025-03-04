using System.Runtime.InteropServices;
using System.Text;
using MUProcessMonitor.Context;
using MUProcessMonitor.Services;

namespace MUProcessMonitor.Forms;

public class MUWindowListForm : Form
{
    private ListView listView;
    private Button btnRefresh, btnConfigure, btnStopAlarm;
    private TrayApplicationContextWindows trayContext;
    private ContextMenuStrip contextMenuStrip;
    private HelperMonitorService helperMonitorService;
    private Dictionary<string, Bitmap> screenshotCache;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public MUWindowListForm(TrayApplicationContextWindows context)
    {
        trayContext = context;
        helperMonitorService = new HelperMonitorService();
        screenshotCache = new Dictionary<string, Bitmap>();
        Text = "MU Window Monitor";
        Width = 500;
        Height = 400;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += trayContext.OnExit;
        Resize += WindowListForm_Resize;

        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string resourcePath = Path.Combine(basePath, "Resources", "icon_mupm.ico");
        Icon = new Icon(resourcePath);

        listView = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Width = 465,
            Height = 300,
            Left = 10,
            Top = 10
        };

        listView.Columns.Add("Handle", 100);
        listView.Columns.Add("Title", 150);
        listView.Columns.Add("Status", 100);

        listView.MouseClick += ListView_MouseClick;

        contextMenuStrip = new ContextMenuStrip();

        btnRefresh = new Button { Text = "Refresh", Left = 100, Width = 100, Top = 320 };
        btnRefresh.Click += (s, e) => LoadWindows();

        btnConfigure = new Button { Text = "Configure", Left = 210, Width = 100, Top = 320 };
        btnConfigure.Click += (s, e) => trayContext.OnConfigure(s, e);

        btnStopAlarm = new Button { Text = "Stop Alarm", Left = 320, Width = 100, Top = 320 };
        btnStopAlarm.Click += (s, e) => trayContext.StopAlarm(s, e);

        Controls.Add(listView);
        Controls.Add(btnRefresh);
        Controls.Add(btnConfigure);
        Controls.Add(btnStopAlarm);

        LoadWindows();
    }

    public void SafeLoadWindows()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(LoadWindows));
        }
        else
        {
            LoadWindows();
        }
    }

    private void LoadWindows()
    {
        listView.Items.Clear();
        screenshotCache.Clear();

        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                var windowText = new StringBuilder(256);
                GetWindowText(hWnd, windowText, windowText.Capacity);

                if (windowText.ToString().Equals("MU", StringComparison.OrdinalIgnoreCase))
                {
                    var status = trayContext.IsMonitoring((int)hWnd) ? "Monitoring" : "Not Monitoring";
                    var windowRect = helperMonitorService.GetWindowRectangle((int)hWnd);
                    var screenshot = helperMonitorService.CaptureScreen(windowRect);

                    var item = new ListViewItem(hWnd.ToString());
                    item.SubItems.Add(windowText.ToString());
                    item.SubItems.Add(status);

                    if (screenshot != null)
                        screenshotCache[hWnd.ToString()] = screenshot;

                    listView.Items.Add(item);
                }
            }
            return true;
        }, IntPtr.Zero);
    }

    private void ListView_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && listView.FocusedItem != null && listView.FocusedItem.Bounds.Contains(e.Location))
        {
            contextMenuStrip.Items.Clear();

            int handle = int.Parse(listView.FocusedItem.Text);
            bool isMonitoring = trayContext.IsMonitoring(handle);

            if (!isMonitoring)
            {
                var startItem = new ToolStripMenuItem("Start Monitoring");
                startItem.Click += (s, ev) => StartMonitoring(handle);
                contextMenuStrip.Items.Add(startItem);
            }
            else
            {
                var stopItem = new ToolStripMenuItem("Stop Monitoring");
                stopItem.Click += (s, ev) => StopMonitoring(handle);
                contextMenuStrip.Items.Add(stopItem);
            }

            if (screenshotCache.ContainsKey(handle.ToString()))
            {
                var viewScreenshotItem = new ToolStripMenuItem("View Screenshot");
                viewScreenshotItem.Click += (s, ev) => ShowScreenshot(screenshotCache[handle.ToString()]);
                contextMenuStrip.Items.Add(viewScreenshotItem);
            }

            contextMenuStrip.Show(Cursor.Position);
        }
    }

    private void ShowScreenshot(Bitmap screenshot)
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string resourcePath = Path.Combine(basePath, "Resources", "icon_mupm.ico");

        var screenshotForm = new Form
        {
            Text = "Screenshot Preview",
            Width = screenshot.Width + 20,
            Height = screenshot.Height + 40,
            StartPosition = FormStartPosition.CenterScreen,
            Icon = new Icon(resourcePath)
        };

        var pictureBox = new PictureBox
        {
            Image = screenshot,
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.StretchImage,
        };

        screenshotForm.Controls.Add(pictureBox);
        screenshotForm.ShowDialog();
    }

    private void StartMonitoring(int handle)
    {
        trayContext.StartMonitoring(handle);
        LoadWindows();
    }

    private void StopMonitoring(int handle)
    {
        trayContext.StopMonitoring(handle);
        LoadWindows();
    }

    private void WindowListForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
    }
}
