using System.Windows;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Application subclass every gadget entry point uses: installs the light menu /
/// options styling app-wide before any window is created.
/// </summary>
public class GadgetApplication : Application
{
    static GadgetApplication()
    {
        MenuStyle.Apply(Current!);
    }
}
