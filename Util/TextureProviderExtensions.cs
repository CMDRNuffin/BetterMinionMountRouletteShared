namespace BetterRouletteBase.Util;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

internal static class TextureProviderExtensions
{
    public static ImTextureID LoadUldTexture(this ITextureProvider textureProvider, string name)
    {
        string path = $"ui/uld/{name}_hr1.tex";
        return textureProvider.GetFromGame(path).GetWrapOrEmpty().Handle;
    }

    public static ImTextureID LoadIconTexture(this ITextureProvider textureProvider, uint id)
    {
        return textureProvider.GetFromGameIcon(new GameIconLookup(id)).GetWrapOrEmpty().Handle;
    }
}
