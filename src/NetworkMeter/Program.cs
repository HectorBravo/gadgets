using System.Windows;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.NetworkMeter;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new GadgetApplication { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Run(new MainWindow());
    }
}

