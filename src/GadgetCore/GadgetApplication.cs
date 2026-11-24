using System.Windows;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Application subclass every gadget entry point uses: installs the light menu /
/// options styling app-wide before any window is created. Must run from an instance
/// (static ctor runs before `Current` exists), so it hooks its own constructor.
/// </summary>
public class GadgetApplication : Application
{
    public GadgetApplication()
    {
        MenuStyle.Apply(this);
    }
}
