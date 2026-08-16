using PlcEmulator.Config;
using PlcEmulator.Core.Instructions;

namespace PlcEmulator.Core;

/// <summary>
/// Builds the runtime <see cref="TagTable"/> (DATA-IN-100) and
/// <see cref="Rung"/> program (DATA-IN-101) from a validated
/// <see cref="ControlLogicDef"/>. Used by <see cref="PlcController"/>'s
/// constructor, and directly unit-testable on its own — building the
/// tag/rung model needs only the CONTROL_LOGIC definition, not the
/// NETWORK/driver wiring a full <see cref="PlcController"/> also needs
/// (see docs/IMPLEMENTATION_PLAN.md, item 2).
/// </summary>
public static class ControlLogicBuilder
{
    /// <summary>Populates a new <see cref="TagTable"/> with every tag CONTROL_LOGIC defines, at its declared initial value/preset.</summary>
    public static TagTable BuildTagTable(ControlLogicDef controlLogic)
    {
        var table = new TagTable();

        foreach (var tagDef in controlLogic.Tags)
        {
            table.Define(CreateTag(tagDef));
        }

        return table;
    }

    /// <summary>Builds the ordered rung/instruction program CONTROL_LOGIC defines.</summary>
    public static IReadOnlyList<Rung> BuildRungs(ControlLogicDef controlLogic)
    {
        var rungs = new List<Rung>(controlLogic.Rungs.Count);

        foreach (var rungDef in controlLogic.Rungs)
        {
            var instructions = new List<IInstruction>(rungDef.Instructions.Count);

            foreach (var instructionDef in rungDef.Instructions)
            {
                instructions.Add(InstructionFactory.Create(instructionDef));
            }

            rungs.Add(new Rung { Instructions = instructions });
        }

        return rungs;
    }

    private static Tag CreateTag(TagDef def)
    {
        var type = ToRuntimeType(def.Type);
        var tag = new Tag { Name = def.Name, Type = type };

        switch (type)
        {
            case TagType.Timer:
                tag.Timer = new TimerState { Pre = def.Preset ?? 0, Acc = 0, Dn = false, En = false };
                break;
            case TagType.Counter:
                tag.Counter = new CounterState { Pre = def.Preset ?? 0, Acc = 0, Dn = false };
                break;
            default:
                tag.Value = def.InitialValue;
                break;
        }

        return tag;
    }

    private static TagType ToRuntimeType(TagTypeDef type) => type switch
    {
        TagTypeDef.Bool => TagType.Bool,
        TagTypeDef.Dint => TagType.Dint,
        TagTypeDef.Real => TagType.Real,
        TagTypeDef.Timer => TagType.Timer,
        TagTypeDef.Counter => TagType.Counter,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unrecognized CONTROL_LOGIC tag type."),
    };
}
