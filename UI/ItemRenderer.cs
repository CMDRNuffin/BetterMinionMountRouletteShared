namespace BetterRouletteBase.UI;

using BetterRouletteBase.Config;
using BetterRouletteBase.Util;

using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

internal sealed class ItemRenderer<TItem, TGroup>(ITextureProvider textureProvider)
    where TItem : ItemData
    where TGroup : IItemGroup
{
    private const int PAGE_SIZE = COLUMNS * ROWS;
    private const int COLUMNS = 5;
    private const int ROWS = 6;

    private readonly ITextureProvider _textureProvider = textureProvider;

    public void RenderPage(List<TItem> mounts, TGroup group, int page)
    {
        int i = 0;
        foreach (TItem item in mounts.Skip((page - 1) * PAGE_SIZE).Take(PAGE_SIZE))
        {
            if (i++ > 0)
            {
                ImGui.SameLine();
            }

            if (i >= COLUMNS)
            {
                i = 0;
            }

            bool enabled = group.IncludedItems.Contains(item.ID) == group.IncludedMeansActive;
            enabled = Render(item, enabled);
            _ = enabled == group.IncludedMeansActive
                ? group.IncludedItems.Add(item.ID)
                : group.IncludedItems.Remove(item.ID);
        }
    }

    public static void Update(List<TItem> items, TGroup group, bool selected, int? page)
    {
        int start, end;
        if (page is not null)
        {
            start = (page.Value - 1) * PAGE_SIZE;
            end = start + PAGE_SIZE;
        }
        else
        {
            start = 0;
            end = items.Count;
        }

        HashSet<uint> selectedItems = group.IncludedItems;

        Func<uint, bool> selectOperation = selected == group.IncludedMeansActive
            ? selectedItems.Add
            : selectedItems.Remove;

        for (int i = start; i < end; ++i)
        {
            TItem item = items[i];
            _ = selectOperation(item.ID);
        }
    }

    public static int GetPageCount(int itemCount)
    {
        return (itemCount / PAGE_SIZE) + (itemCount % PAGE_SIZE == 0 ? 0 : 1);
    }

    public bool Render(TItem itemData, bool enabled)
    {
        ImTextureID selectedUnselectedIcon = _textureProvider.LoadUldTexture("readycheck");

        ImTextureID mountIcon = itemData.GetIcon();

        _ = ImGui.TableNextColumn();

        Vector2 originalPos = ImGui.GetCursorPos();

        const float BUTTON_SIZE = 60f;
        const float OVERLAY_SIZE = 24f;
        const float OVERLAY_OFFSET = 4f;
        var buttonSize = new Vector2(BUTTON_SIZE);
        var overlaySize = new Vector2(OVERLAY_SIZE);

        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);

        if (ImGui.ImageButton(mountIcon, buttonSize, Vector2.Zero, Vector2.One, 0))
        {
            enabled ^= true;
        }

        ImGui.PopStyleColor(3);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(StringCache.Items[itemData.ID, () => itemData.CapitalizedName]);
        }

        Vector2 finalPos = ImGui.GetCursorPos();

        // calculate overlay (top right corner) position
        Vector2 overlayPos = originalPos + new Vector2(buttonSize.X - overlaySize.X + OVERLAY_OFFSET, 0);
        ImGui.SetCursorPos(overlayPos);

        Vector2 offset = new(enabled ? 0.1f : 0.6f, 0.2f);
        Vector2 offset2 = new(enabled ? 0.4f : 0.9f, 0.8f);
        ImGui.Image(selectedUnselectedIcon, overlaySize, offset, offset2);

        // put cursor back to where it was after rendering the button to prevent
        // messing up the table rendering
        ImGui.SetCursorPos(finalPos);

        return enabled;
    }
}
