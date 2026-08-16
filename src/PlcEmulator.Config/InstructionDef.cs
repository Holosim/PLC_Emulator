namespace PlcEmulator.Config;

/// <summary>
/// One parsed ladder-logic instruction (DATA-IN-101): a mnemonic drawn
/// from the MVP instruction set (<c>XIC</c>, <c>XIO</c>, <c>OTE</c>,
/// <c>TON</c>, <c>TOF</c>, <c>CTU</c>, <c>CTD</c>, <c>RES</c>,
/// <c>EQU</c>, <c>NEQ</c>, <c>GRT</c>, <c>LES</c>, <c>GEQ</c>,
/// <c>LEQ</c>, <c>ADD</c>, <c>SUB</c>, <c>MUL</c>, <c>DIV</c>) plus its
/// ordered operands. Mnemonic legality and exact operand arity per
/// mnemonic are enforced by <c>PlcEmulator.Core.Instructions.InstructionFactory</c>
/// when this definition is turned into a real instruction instance —
/// this type only captures what CONTROL_LOGIC JSON said, generically.
/// </summary>
public sealed class InstructionDef
{
    public required string Mnemonic { get; init; }
    public required IReadOnlyList<OperandDef> Operands { get; init; }
}
