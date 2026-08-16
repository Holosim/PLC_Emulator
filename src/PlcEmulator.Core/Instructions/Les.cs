using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Less-than compare. Evaluation semantics land with CORE-207.</summary>
public sealed class Les : CompareInstruction
{
    public Les(OperandDef left, OperandDef right) : base(left, right)
    {
    }

    public override string Mnemonic => "LES";
}
