namespace BetterRouletteBase.UI;

using BetterRouletteBase.Config;
using BetterRouletteBase.Util;
using BetterRouletteBase.Util.Memory;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using System;
using System.Collections.Generic;
using System.Linq;

internal abstract class ItemGroupPage<TItem, TGroup, TRegistry>
    where TItem : ItemData
    where TGroup : IItemGroup
    where TRegistry : ItemRegistry<TItem, TGroup>
{
    private readonly WindowManagerBase _windowManager;
    private readonly ItemRenderer<TItem, TGroup> _itemRenderer;
    private string? _currentItemGroup;
    private ItemGroupPageEnum _mode = ItemGroupPageEnum.Settings;
    private string _nameFilter = "";
    private List<TItem>? _filteredItems;
    private (int UnlockedCount, StringView Text) _lastFilter;
    private readonly ItemGroupPageTexts _texts;

    private enum ItemGroupPageEnum
    {
        Settings,
        Items
    }

    internal ItemGroupPage(TRegistry itemRegistry, ITextureProvider textureProvider, WindowManagerBase windowManager, string itemName)
    {
        ItemRegistry = itemRegistry;
        _windowManager = windowManager;
        _itemRenderer = new ItemRenderer<TItem, TGroup>(textureProvider);
        _texts = new(itemName);
    }

    protected TRegistry ItemRegistry { get; }

    public void RenderPage(ICharacterConfig<TGroup> characterConfig)
    {
        TGroup items = SelectCurrentGroup(characterConfig);
        DrawItemGroup(items);
    }

    private void DrawItemGroup(TGroup group)
    {
        if (group is null)
        {
            ImGui.Text("Group is null!"u8);
            return;
        }

        bool isSettingsOpen = _mode == ItemGroupPageEnum.Settings;
        bool isItemsOpen = _mode == ItemGroupPageEnum.Items;
        bool enableNewItems = !group.IncludedMeansActive;

        ImGui.GetStateStorage().SetInt(ImGui.GetID("Settings"u8), isSettingsOpen ? 1 : 0);
        ImGui.BeginDisabled(isSettingsOpen);
        if (ImGui.CollapsingHeader("Settings"u8))
        {
            ImGui.EndDisabled();
            isSettingsOpen = true;
            RenderGroupSettings(group, ref enableNewItems);
        }
        else
        {
            ImGui.EndDisabled();
        }

        List<TItem> unlockedItems = ItemRegistry.GetUnlockedItems();
        UpdateItemSelectionData(group, unlockedItems, enableNewItems);

        ImGui.GetStateStorage().SetInt(ImGui.GetID(_texts.ItemListHeaderLabel), isItemsOpen ? 1 : 0);
        ImGui.BeginDisabled(isItemsOpen);
        if (ImGui.CollapsingHeader(_texts.ItemListHeaderLabel))
        {
            ImGui.EndDisabled();
            isItemsOpen = true;

            StringView nameFilter = new();
            if (unlockedItems.Count > 0)
            {
                nameFilter = DrawNameFilter();
            }

            List<TItem> filteredAndUnlockedItems = ApplyFilterAndGetFilteredItems(unlockedItems, nameFilter);

            int pages = ItemRenderer<TItem, TGroup>.GetPageCount(filteredAndUnlockedItems.Count);
            if (pages == 0)
            {
                ImGui.Text(
                    unlockedItems.Count == 0
                        ? _texts.NoItemsUnlockedLabel
                        : _texts.NoItemsInFilterLabel
                );
            }
            else if (ImGui.BeginTabBar(_texts.PagesTabBarId))
            {
                for (int page = 1; page <= pages; page++)
                {
                    if (ImGui.BeginTabItem(StringCache.Pages[page, () => $"{page}"]))
                    {
                        RenderItemListPage(page, group, filteredAndUnlockedItems);
                        ImGui.EndTabItem();
                    }
                }

                ImGui.SameLine();
                ImGui.EndTabBar();
            }
        }
        else
        {
            ImGui.EndDisabled();
        }

        switch (_mode)
        {
            case ItemGroupPageEnum.Settings when isItemsOpen:
                _mode = ItemGroupPageEnum.Items;
                break;
            case ItemGroupPageEnum.Items when isSettingsOpen:
                _mode = ItemGroupPageEnum.Settings;
                break;
            case ItemGroupPageEnum.Settings:
            case ItemGroupPageEnum.Items:
                break;
            default:
                // Something somewhere went horribly wrong. Reset to settings.
                _mode = ItemGroupPageEnum.Settings;
                break;
        }
    }

    private StringView DrawNameFilter()
    {
        ImGui.SetNextItemWidth(250);

        _ = ImGui.InputTextWithHint("###nameFilter"u8, "Search for name..."u8, ref _nameFilter);

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.FilterCircleXmark))
        {
            _nameFilter = string.Empty;
        }

        if (ImGui.IsItemHovered())
        {
            using (ImRaii.Tooltip())
            {
                ImGui.SetTooltip("Clear name filter"u8);
            }
        }

        return new StringView(_nameFilter).Trim();
    }

    private void RenderItemListPage(int page, TGroup group, List<TItem> unlockedAndFilteredItems)
    {
        _itemRenderer.RenderPage(unlockedAndFilteredItems, group, page);

        (bool Select, int? Page)? maybeInfo = null;

        Button("Select all"u8, ref maybeInfo, (true, null));
        ImGui.SameLine();
        Button("Unselect all"u8, ref maybeInfo, (false, null));
        ImGui.SameLine();
        Button("Select page"u8, ref maybeInfo, (true, page));
        ImGui.SameLine();
        Button("Unselect page"u8, ref maybeInfo, (false, page));

        if (maybeInfo is { } info)
        {
            string selectText = info.Select ? "select" : "unselect";
            string pageInfo = (info.Page, info.Select) switch
            {
                (null, true) => _nameFilter.IsNullOrEmpty()
                    ? _texts.AllUnselectedItemsLabel
                    : $"{_texts.ItemName}s matching \"{_nameFilter}\"",
                (null, false) => _nameFilter.IsNullOrEmpty()
                    ? _texts.AllSelectedItemsLabel
                    : $"{_texts.ItemName}s matching \"{_nameFilter}\"",
                _ => _texts.CurrentPageItemsLabel,
            };

            _windowManager.ConfirmYesNo(
                "Are you sure?",
                $"Do you really want to {selectText} all {pageInfo}?",
                () => ItemRenderer<TItem, TGroup>.Update(
                    unlockedAndFilteredItems,
                    group,
                    info.Select,
                    info.Page
                )
            );
        }

        static void Button(ReadOnlySpan<byte> label, ref (bool, int?)? maybeInfo, (bool, int?) value)
        {
            if (ImGui.Button(label))
            {
                maybeInfo = value;
            }
        }
    }

    protected abstract void PluginSpecificSettings(TGroup group);

    private void RenderGroupSettings(TGroup group, ref bool enableNewItems)
    {
        _ = ImGui.Checkbox(_texts.EnableNewItemsLabel, ref enableNewItems);

        PluginSpecificSettings(group);
    }

    private List<TItem> ApplyFilterAndGetFilteredItems(List<TItem> unlockedItems, StringView filter)
    {
        if (filter.Length == 0)
        {
            return unlockedItems;
        }

        if (_filteredItems is null
            || unlockedItems.Count != _lastFilter.UnlockedCount
            || !filter.Equals(_lastFilter.Text, StringComparison.OrdinalIgnoreCase))
        {
            _filteredItems = unlockedItems
                .Where(
                    itemData => itemData.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
        }

        _lastFilter = (UnlockedCount: unlockedItems.Count, Text: filter);

        return _filteredItems;
    }

    private static void UpdateItemSelectionData(TGroup group, List<TItem> unlockedItems, bool enableNewItems)
    {
        if (enableNewItems == group.IncludedMeansActive)
        {
            // we auto-enable new items by tracking which items are explicitly disabled
            group.IncludedMeansActive = !enableNewItems;

            // invert selection
            var unlockedItemIDs = unlockedItems.Select(x => x.ID).ToHashSet();
            unlockedItemIDs.ExceptWith(group.IncludedItems);
            group.IncludedItems.Clear();
            group.IncludedItems.UnionWith(unlockedItemIDs);
        }
    }

    private TGroup SelectCurrentGroup(ICharacterConfig<TGroup> characterConfig)
    {
        if (_currentItemGroup is not null && characterConfig.Groups.All(x => x.Name != _currentItemGroup))
        {
            _currentItemGroup = null;
        }

        _currentItemGroup ??= characterConfig.Groups.First().Name;

        ControlHelper.SelectItem(characterConfig.Groups, x => x.Name, ref _currentItemGroup, "##currentgroup"u8, 150);

        string currentGroup = _currentItemGroup;
        ImGui.SameLine();
        if (ImGui.Button("Add"u8))
        {
            var dialog = new RenameItemDialog(
                "Add a new group",
                string.Empty,
                x => AddItemGroup(characterConfig, x)
            ) { NormalizeWhitespace = true };

            dialog.SetValidation(CreateValidator(characterConfig, isNew: true), x => "A group with that name already exists."u8);
            _windowManager.OpenDialog(dialog);
        }

        ImGui.SameLine();
        if (ImGui.Button("Edit"))
        {
            var dialog = new RenameItemDialog(
                $"Rename {_currentItemGroup}",
                _currentItemGroup,
                (newName) => RenameItemGroup(characterConfig, _currentItemGroup, newName)
            ) { NormalizeWhitespace = true };

            dialog.SetValidation(
                CreateValidator(characterConfig, isNew: false),
                x => "Another group with that name already exists."u8
            );

            _windowManager.OpenDialog(dialog);
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!characterConfig.HasNonDefaultGroups);
        if (ImGui.Button("Delete"))
        {
            _windowManager.Confirm(
                $"Confirm deletion of {_texts.ItemName} group",
                $"Are you sure you want to delete {currentGroup}?\nThis action can NOT be undone.",
                ("OK", () => DeleteItemGroup(characterConfig, currentGroup)),
                "Cancel"
            );
        }

        ImGui.EndDisabled();

        return characterConfig.GetGroupByName(_currentItemGroup)!;

        Func<StringView, bool> CreateValidator(ICharacterConfig<TGroup> characterConfig, bool isNew)
        {
            HashSet<StringView> names = new(
                characterConfig.Groups.Select(x => new StringView(x.Name)),
                StringViewComparer.InvariantCultureIgnoreCase
            );

            if (!isNew)
            {
                _ = names.Remove(currentGroup);
            }

            return newName => !names.Contains(newName);
        }
    }

    private void DeleteItemGroup(ICharacterConfig<TGroup> characterConfig, string name)
    {
        ItemGroupManager<TGroup>.Delete(characterConfig, name);

        if (_currentItemGroup == name)
        {
            _currentItemGroup = null;
        }
    }

    private void RenameItemGroup(ICharacterConfig<TGroup> characterConfig, string currentItemGroup, string newName)
    {
        ItemGroupManager<TGroup>.Rename(characterConfig, currentItemGroup, newName);

        if (_currentItemGroup == currentItemGroup)
        {
            _currentItemGroup = newName;
        }
    }

    private void AddItemGroup(ICharacterConfig<TGroup> characterConfig, string name)
    {
        characterConfig.AddGroup(name);
        _currentItemGroup = name;
    }
}

internal readonly struct ItemGroupPageTexts(string itemName)
{
    public readonly string ItemListHeaderLabel = char.ToUpperInvariant(itemName[0]) + itemName[1..];
    public readonly string NoItemsUnlockedLabel = $"Please unlock at least one {itemName}.";
    public readonly string NoItemsInFilterLabel = $"No {itemName}s match the current filter.";
    public readonly string PagesTabBarId = $"{itemName}_pages";
    public readonly string ItemName = itemName;
    public readonly string AllSelectedItemsLabel = $"currently selected {itemName}s";
    public readonly string AllUnselectedItemsLabel = $"currently unselected {itemName}s";
    public readonly string CurrentPageItemsLabel = $"{itemName}s on the current page";
    public readonly string EnableNewItemsLabel = $"Enable new {itemName}s on unlock";
}