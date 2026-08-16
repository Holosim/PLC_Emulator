namespace PlcEmulator.Core;

/// <summary>
/// Structured sub-elements of a timer tag (<c>TON</c>/<c>TOF</c>),
/// per DATA-IN-100: <c>.PRE</c> (preset, ms), <c>.ACC</c> (accumulated,
/// ms), <c>.DN</c> (done bit), <c>.EN</c> (enable bit).
/// </summary>
public sealed class TimerState
{
    public int Pre { get; set; }
    public int Acc { get; set; }
    public bool Dn { get; set; }
    public bool En { get; set; }
}
