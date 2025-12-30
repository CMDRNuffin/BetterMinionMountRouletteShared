namespace BetterRouletteBase.Config;

using System.Collections.Generic;

internal interface IItemGroup
{
    public string Name { get; set; }
    HashSet<uint> IncludedItems { get; }
    bool IncludedMeansActive { get; set; }
}
