using MUProcessMonitor.Context;

namespace MUProcessMonitor;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApplicationContextWindows());
    }
}