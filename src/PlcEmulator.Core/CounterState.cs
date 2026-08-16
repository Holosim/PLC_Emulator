namespace PlcEmulator.Core;

/// <summary>
/// Structured sub-elements of a counter tag (<c>CTU</c>/<c>CTD</c>),
/// per DATA-IN-100: <c>.PRE</c> (preset), <c>.ACC</c> (accumulated),
/// <c>.DN</c> (done bit).
/// </summary>
public sealed class CounterState
{
    public int Pre { get; set; }
    public int Acc { get; set; }
    public bool Dn { get; set; }
}
