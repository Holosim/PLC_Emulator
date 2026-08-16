namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Shared contract for every ladder-logic instruction mnemonic
/// (<c>XIC</c>, <c>OTE</c>, <c>TON</c>, <c>CTU</c>, etc.). Stateless —
/// operates only on the <see cref="TagTable"/> it is given, so the
/// same instruction logic is reused correctly across isolated
/// <see cref="PlcController"/> instances (NFR-500; see docs/SDD.md,
/// Coding Standards / Instruction classes).
/// </summary>
public interface IInstruction
{
    /// <summary>
    /// Evaluates this instruction against the given tag table,
    /// returning the instruction's rung-true/false result where
    /// applicable (e.g. a contact or compare) and applying any side
    /// effects (e.g. a coil write, a timer/counter update).
    /// </summary>
    bool Evaluate(TagTable tags);
}
