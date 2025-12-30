namespace BetterRouletteBase.Config;

using System.Diagnostics.CodeAnalysis;

public class CharacterConfigEntry
{
    [field: MaybeNull]
    public string CharacterName
    {
        get => field ?? "INVALID CONFIG";
        set;
    }

    [field: MaybeNull]
    public string CharacterWorld
    {
        get => field ?? "INVALID CONFIG";
        set;
    }

    public string FileName { get; set; } = "";
}
