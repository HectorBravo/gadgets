using System.Windows;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.CpuMeter;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Run(new MainWindow());
    }
}
