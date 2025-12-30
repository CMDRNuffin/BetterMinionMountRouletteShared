namespace BetterRouletteBase.Config;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Newtonsoft.Json;

using System.Collections.Generic;
using System.IO;

internal abstract class CharacterManagerBase<TConfiguration, TCharacterConfig>(
    IPluginLog pluginLog,
    IDalamudPluginInterface dalamudPluginInterface,
    IPlayerState playerState,
    TConfiguration configuration
)
    : ICharacterManager
    where TConfiguration : ConfigurationBase
{
    private readonly TConfiguration _configuration = configuration;

    protected ulong? PlayerID { get; set; }

    protected IPluginLog PluginLog { get; } = pluginLog;
    protected IDalamudPluginInterface DalamudPluginInterface { get; } = dalamudPluginInterface;
    protected IPlayerState PlayerState { get; } = playerState;
    protected TCharacterConfig? CharacterConfig { get; set; }

    public bool Import(ulong fromPlayerID)
    {
        PluginLog.Debug($"Importing {fromPlayerID}");
        if (fromPlayerID == PlayerID || PlayerID is not ulong currentPlayer)
        {
            PluginLog.Debug($"No use importing from current character");
            // importing from yourself is a noop and should therefore always succeed
            return true;
        }

        TCharacterConfig? configToImport = LoadCharacterConfig(fromPlayerID);
        if (configToImport is null || CharacterConfig is null)
        {
            List<string> items = [];
            if (configToImport is null)
            {
                items.Add("imported config is null");
            }

            if (CharacterConfig is null)
            {
                items.Add("current config is null");
            }

            PluginLog.Debug($"Unable to import: {string.Join(", ", items)}");

            return false;
        }

        CharacterConfigEntry cce = _configuration.CharacterConfigs[currentPlayer];

        ImportFromConfig(CharacterConfig, configToImport);
        SaveCurrentCharacterConfig(cce);

        PluginLog.Debug($"Import successful");
        return true;
    }

    public TCharacterConfig GetCharacterConfig(ulong playerID)
    {
        if (CharacterConfig is { } cfg && playerID == PlayerID)
        {
            return cfg;
        }

        PlayerID = playerID;
        CharacterConfig = default;
        if (_configuration.CharacterConfigs.TryGetValue(playerID, out CharacterConfigEntry? cce))
        {
            CharacterConfig = LoadCharacterConfig(cce);
        }

        if (CharacterConfig is null)
        {
            CharacterConfig = CreateCharacterConfig();
            cce = new CharacterConfigEntry
            {
                CharacterName = PlayerState.CharacterName,
                CharacterWorld = PlayerState.HomeWorld.Value.Name.ExtractText() ?? "",
            };

            cce.FileName = $"{playerID}_{cce.CharacterName.Replace(' ', '_')}@{cce.CharacterWorld}.json";
            _configuration.CharacterConfigs[playerID] = cce;

            SaveCurrentCharacterConfig(cce);
            DalamudPluginInterface.SavePluginConfig(_configuration);
        }

        return CharacterConfig;
    }

    public void SaveCurrentCharacterConfig()
    {
        if (PlayerID is not ulong playerID)
        {
            return;
        }

        CharacterConfigEntry cce = _configuration.CharacterConfigs[playerID];
        SaveCurrentCharacterConfig(cce);
    }

    protected abstract void ImportFromConfig(TCharacterConfig current, TCharacterConfig toImport);

    protected abstract TCharacterConfig CreateCharacterConfig();

    protected virtual TCharacterConfig? LoadCharacterConfig(ulong playerID, CharacterConfigEntry cce)
    {
        return LoadCharacterConfig(cce);
    }

    protected TCharacterConfig? LoadCharacterConfig(ulong playerID)
    {
        return _configuration.CharacterConfigs.TryGetValue(playerID, out CharacterConfigEntry? cce)
            ? LoadCharacterConfig(playerID, cce)
            : default;
    }

    protected TCharacterConfig? LoadCharacterConfig(CharacterConfigEntry cce)
    {
        if (cce.FileName is not null /* can still be null if freshly loaded */)
        {
            string path = Path.Combine(GetCharConfigDir(), cce.FileName);

            if (File.Exists(path))
            {
                try
                {
                    return JsonConvert.DeserializeObject<TCharacterConfig>(File.ReadAllText(path));
                }
                catch (IOException /* file deleted in the meantime. shouldn't happen, but technically can */)
                {
                }
            }
        }

        return default;
    }

    private void SaveCurrentCharacterConfig(CharacterConfigEntry entry)
    {
        if (CharacterConfig is { } charConfig)
        {
            SaveCharacterConfig(entry, charConfig);
        }
    }

    private void SaveCharacterConfig(CharacterConfigEntry entry, TCharacterConfig? config)
    {
        if (config is null)
        {
            return;
        }

        string dir = GetCharConfigDir();
        _ = Directory.CreateDirectory(dir);

        File.WriteAllText(Path.Combine(dir, entry.FileName), JsonConvert.SerializeObject(config));
    }

    private string GetCharConfigDir()
    {
        return DalamudPluginInterface.GetPluginConfigDirectory();
    }
}