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
    /// The instruction's mnemonic (<c>XIC</c>, <c>OTE</c>, <c>TON</c>,
    /// etc.), preserved verbatim from CONTROL_LOGIC JSON (DATA-IN-101;
    /// see docs/SDD.md, Coding Standards / Naming). Used for
    /// diagnostics/logging (UI-002) and for inspecting a parsed rung's
    /// instruction sequence (e.g. TP-101).
    /// </summary>
    string Mnemonic { get; }

    /// <summary>
    /// Evaluates this instruction against the given tag table,
    /// returning the instruction's rung-true/false result where
    /// applicable (e.g. a contact or compare) and applying any side
    /// effects (e.g. a coil write, a timer/counter update).
    /// </summary>
    bool Evaluate(TagTable tags);
}
