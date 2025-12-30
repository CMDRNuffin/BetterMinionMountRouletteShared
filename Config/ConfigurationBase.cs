namespace BetterRouletteBase.Config;

using Dalamud.Configuration;

using System.Collections.Generic;

internal class ConfigurationBase : IPluginConfiguration
{

    [Versions(introduced: 0)]
    public int Version { get; set; }

    [Versions(introduced: 3)]
    public Dictionary<ulong, CharacterConfigEntry> CharacterConfigs { get; set; } = [];
}
