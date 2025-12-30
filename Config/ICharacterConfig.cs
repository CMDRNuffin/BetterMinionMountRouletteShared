namespace BetterRouletteBase.Config;

using BetterRouletteBase.Util.Memory;

using System.Collections.Generic;

internal interface ICharacterConfig<TGroup> where TGroup : IItemGroup
{
    List<TGroup> Groups { get; }
    bool HasNonDefaultGroups { get; }

    void ResetSelection(string from, string? to);
    void AddGroup(string name);
    TGroup? GetGroupByName(StringView name);
}
