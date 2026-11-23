using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DigitalClock;

/// <summary>Where the weekday name is shown on the date line.</summary>
public enum WeekdayPos { None, Before, After }

/// <summary>Digital Clock gadget settings.</summary>
public class ClockSettings : BaseSettings
{
    public bool Use12h { get; set; } = false;
    public bool ShowSeconds { get; set; } = true;
    public bool ShowDate { get; set; } = true;
    public WeekdayPos Weekday { get; set; } = WeekdayPos.None;
    public bool ShowDayOfYear { get; set; } = true;
    public bool ShowWeek { get; set; } = true;
    public bool ShowQuarter { get; set; } = true;
    public bool ShowUptime { get; set; } = true;
}
