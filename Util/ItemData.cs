namespace BetterRouletteBase.Util;

using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Text.ReadOnly;

using System.Globalization;

internal abstract class ItemData
{
    private readonly ITextureProvider _textureProvider;
    private readonly ReadOnlySeString _name;

    public ItemData(ITextureProvider textureProvider, ReadOnlySeString name)
    {
        _textureProvider = textureProvider;
        _name = name;
    }

    public uint ID { get; init; }

    public uint IconID { get; init; }

    public string Name => field ??= _name.ExtractText();

    public bool Unlocked { get; set; }

    public string CapitalizedName => field ??= CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Name);

    public ImTextureID GetIcon()
    {
        return _textureProvider.LoadIconTexture(IconID);
    }

    public abstract bool IsAvailable(Pointer<ActionManager> actionManager);
}
