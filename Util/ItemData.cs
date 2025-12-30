namespace BetterRouletteBase.Util;

using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Text.ReadOnly;

using System.Globalization;

internal abstract class ItemData(ITextureProvider textureProvider, ReadOnlySeString name)
{
    private readonly ITextureProvider _textureHelper = textureProvider;
    private readonly ReadOnlySeString _internalName = name;

    public uint ID { get; init; }

    public uint IconID { get; init; }

    public string Name => field ??= _internalName.ExtractText();

    public bool Unlocked { get; set; }

    public string CapitalizedName => field ??= CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Name);

    public ImTextureID GetIcon()
    {
        return _textureHelper.LoadIconTexture(IconID);
    }

    public abstract bool IsAvailable(Pointer<ActionManager> actionManager);
}
