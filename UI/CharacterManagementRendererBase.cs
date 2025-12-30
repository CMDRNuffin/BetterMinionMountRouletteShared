namespace BetterRouletteBase.UI;

using BetterRouletteBase.Config;
using BetterRouletteBase.Util;

using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

internal abstract class CharacterManagementRendererBase<TConfig>(
    IPlayerState playerState,
    IDalamudPluginInterface dalamudPluginInterface,
    WindowManagerBase windowManager,
    ICharacterManager characterManager,
    TConfig configuration
)
    where TConfig : ConfigurationBase
{
    private readonly IPlayerState _playerState = playerState;
    private readonly IDalamudPluginInterface _dalamudPluginInterface = dalamudPluginInterface;
    private readonly WindowManagerBase _windowManager = windowManager;
    private readonly ICharacterManager _characterManager = characterManager;
    private readonly TConfig _configuration = configuration;
    private ulong? _currentCharacter;

    public void Draw()
    {
        PluginSpecificSettings(_configuration);

        ImGui.Text("Existing characters"u8);
        if (!ImGui.BeginListBox("##Characters"u8))
        {
            return;
        }

        ReadOnlySpan<byte> selectedCharacterName = null;
        foreach (KeyValuePair<ulong, CharacterConfigEntry> character in _configuration.CharacterConfigs.OrderBy(x => x.Key))
        {
            ReadOnlySpan<byte> text = StringCache.Characters[character.Key, () => FormatCharacter(character.Value)];

            if (ImGui.Selectable(text, _currentCharacter == character.Key))
            {
                Util.Toggle(ref _currentCharacter, character.Key);
            }

            if (_currentCharacter == character.Key)
            {
                selectedCharacterName = text;
            }
        }

        ImGui.EndListBox();
        ImGui.BeginDisabled(_currentCharacter is null || _currentCharacter == _playerState.ContentId);

        if (ImGui.Button("Import"))
        {
            Debug.Assert(_currentCharacter is not null);
            ulong currentCharacter = _currentCharacter.Value;
            _windowManager.Confirm(
                "Import settings?",
                $"Import settings from {Encoding.UTF8.GetString(selectedCharacterName)}? This will overwrite all settings for this character!",
                ("Confirm", () => ImportFromCharacter(currentCharacter)),
                "Cancel");
        }

        ImGui.SameLine();

        ImGui.BeginDisabled(IsPredefinedEntry(_currentCharacter));
        if (ImGui.Button("Delete"))
        {
            Debug.Assert(_currentCharacter is not null);
            ulong currentCharacter = _currentCharacter.Value;
            _windowManager.Confirm(
                "Delete settings?",
                $"Delete settings for {Encoding.UTF8.GetString(selectedCharacterName)}? This action cannot be undone!",
                ("Confirm", () => DeleteCharacter(currentCharacter)),
                "Cancel");
        }

        if (IsPredefinedEntry(_currentCharacter))
        {
            ImGui.SameLine();
            ImGui.Text("This configuration cannot be deleted."u8);
        }
        else if (_currentCharacter == _playerState.ContentId)
        {
            ImGui.SameLine();
            ImGui.Text("You cannot import from or delete the currently active character."u8);
        }

        ImGui.EndDisabled();
        ImGui.EndDisabled();
    }

    protected virtual void PluginSpecificSettings(TConfig configuration)
    {
    }

    protected virtual bool IsPredefinedEntry(ulong? characterId)
    {
        return false;
    }

    private static string FormatCharacter(CharacterConfigEntry entry)
    {
        StringBuilder sb = new(entry.CharacterName);
        if (!string.IsNullOrWhiteSpace(entry.CharacterWorld))
        {
            _ = sb.Append(CultureInfo.CurrentCulture, $" ({entry.CharacterWorld})");
        }

        return sb.ToString();
    }

    private void ImportFromCharacter(ulong characterID)
    {
        if (_characterManager.Import(characterID))
        {
            _windowManager.Confirm("Import", "Import successful!", "OK");
        }
        else
        {
            _windowManager.Confirm("Import", "Import failed: Unable to access character config.", "OK");
        }
    }

    private void DeleteCharacter(ulong characterID)
    {
        if (_configuration.CharacterConfigs.TryGetValue(characterID, out CharacterConfigEntry? cce))
        {
            _ = _configuration.CharacterConfigs.Remove(characterID);
            if (cce is not null && !IsPredefinedEntry(characterID))
            {
                try
                {
                    File.Delete(Path.Combine(_dalamudPluginInterface.GetPluginConfigDirectory(), cce.FileName));
                }
                catch (IOException)
                {
                }
            }

            _dalamudPluginInterface.SavePluginConfig(_configuration);
        }
    }
}
