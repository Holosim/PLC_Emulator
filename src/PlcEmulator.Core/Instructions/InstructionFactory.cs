using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Turns a generically-parsed <see cref="InstructionDef"/> (DATA-IN-101)
/// into the concrete <see cref="IInstruction"/> its mnemonic names,
/// enforcing the MVP instruction set and each mnemonic's exact operand
/// arity. This is the single source of truth for both — CONTROL_LOGIC
/// JSON parsing itself (<c>PlcEmulator.Config.ConfigLoader</c>) stays
/// generic about mnemonics/operands so <c>PlcEmulator.Config</c> never
/// needs to know about <c>PlcEmulator.Core</c>'s instruction classes
/// (Config is a leaf project — see docs/SDD.md, Coding Standards).
/// </summary>
public static class InstructionFactory
{
    public static IInstruction Create(InstructionDef def)
    {
        return def.Mnemonic switch
        {
            "XIC" => new Xic(RequireSingleTag(def)),
            "XIO" => new Xio(RequireSingleTag(def)),
            "OTE" => new Ote(RequireSingleTag(def)),
            "TON" => new Ton(RequireSingleTag(def)),
            "TOF" => new Tof(RequireSingleTag(def)),
            "CTU" => new Ctu(RequireSingleTag(def)),
            "CTD" => new Ctd(RequireSingleTag(def)),
            "RES" => new Res(RequireSingleTag(def)),
            "EQU" => Create(def, static (l, r) => new Equ(l, r)),
            "NEQ" => Create(def, static (l, r) => new Neq(l, r)),
            "GRT" => Create(def, static (l, r) => new Grt(l, r)),
            "LES" => Create(def, static (l, r) => new Les(l, r)),
            "GEQ" => Create(def, static (l, r) => new Geq(l, r)),
            "LEQ" => Create(def, static (l, r) => new Leq(l, r)),
            "ADD" => Create(def, static (l, r, d) => new Add(l, r, d)),
            "SUB" => Create(def, static (l, r, d) => new Sub(l, r, d)),
            "MUL" => Create(def, static (l, r, d) => new Mul(l, r, d)),
            "DIV" => Create(def, static (l, r, d) => new Div(l, r, d)),
            _ => throw new ConfigValidationException(
                $"Unrecognized instruction mnemonic '{def.Mnemonic}'. Must be one of the MVP instruction " +
                "set: XIC, XIO, OTE, TON, TOF, CTU, CTD, RES, EQU, NEQ, GRT, LES, GEQ, LEQ, ADD, SUB, MUL, DIV (DATA-IN-101)."),
        };
    }

    private static string RequireSingleTag(InstructionDef def)
    {
        if (def.Operands.Count != 1 || !def.Operands[0].IsTagReference)
        {
            throw new ConfigValidationException(
                $"Instruction '{def.Mnemonic}' requires exactly one tag-name operand.");
        }

        return def.Operands[0].TagName!;
    }

    private static IInstruction Create(InstructionDef def, Func<OperandDef, OperandDef, IInstruction> build)
    {
        if (def.Operands.Count != 2)
        {
            throw new ConfigValidationException(
                $"Instruction '{def.Mnemonic}' requires exactly two operands (tag or literal).");
        }

        return build(def.Operands[0], def.Operands[1]);
    }

    private static IInstruction Create(InstructionDef def, Func<OperandDef, OperandDef, string, IInstruction> build)
    {
        if (def.Operands.Count != 3 || !def.Operands[2].IsTagReference)
        {
            throw new ConfigValidationException(
                $"Instruction '{def.Mnemonic}' requires two source operands (tag or literal) followed by a destination tag.");
        }

        return build(def.Operands[0], def.Operands[1], def.Operands[2].TagName!);
    }
}
