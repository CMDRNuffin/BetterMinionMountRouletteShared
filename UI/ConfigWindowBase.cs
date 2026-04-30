namespace BetterRouletteBase.UI;

using BetterRouletteBase.Config;
using BetterRouletteBase.Util;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

using System;
using System.Linq;
using System.Numerics;

internal abstract class ConfigWindowBase<TCharConfig, TGroup, TItem, TRegistry, TConfig> : Window
    where TCharConfig : ICharacterConfig<TGroup>
    where TGroup : IItemGroup
    where TItem : ItemData
    where TRegistry : ItemRegistry<TItem, TGroup>
    where TConfig : ConfigurationBase
{
    private float _windowMinWidth;
    private ItemGroupPage<TItem, TGroup, TRegistry>? _itemGroupPage;
    private CharacterManagementRendererBase<TConfig>? _characterManagementPage;

    protected ConfigWindowBase(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false)
        : base(name, flags, forceMainWindow)
    {
    }
    protected abstract void Save();
    protected abstract WindowManagerBase WindowManager { get; }
    protected abstract TRegistry ItemRegistry { get; }
    protected abstract TCharConfig? CharacterConfig { get; }
    protected abstract ReadOnlySpan<byte> ItemGroupsTabName { get; }
    protected abstract ItemGroupPage<TItem, TGroup, TRegistry> CreateItemGroupPage();
    protected abstract CharacterManagementRendererBase<TConfig> CreateCharacterManagementRenderer();

    public sealed override void OnOpen()
    {
        base.OnOpen();
        ItemRegistry.RefreshUnlocked();
    }

    public sealed override void PreDraw()
    {
        base.PreDraw();

        // if the minimum width somehow becomes NaN, fix it.
        if (float.IsNaN(_windowMinWidth))
        {
            _windowMinWidth = 0;
        }

        ImGui.SetNextWindowSizeConstraints(new Vector2(_windowMinWidth, 0), new Vector2(float.MaxValue, float.MaxValue));
    }
    public sealed override void OnClose()
    {
        base.OnClose();
        Save();
        WindowManager.RemoveWindow(this);
    }

    public sealed override void Draw()
    {
        if (CharacterConfig is not TCharConfig characterConfig)
        {
            ImGui.Text("Please log in first"u8);
        }
        else if (ImGui.BeginTabBar("settings"u8))
        {
            Tab("General"u8, characterConfig, GeneralConfigTab);
            Tab(ItemGroupsTabName, characterConfig, RenderItemGroupPage);
            Tab("Character Management"u8, characterConfig, RenderCharacterManagementPage);

            ImGui.EndTabBar();
        }

        _windowMinWidth = ImGui.GetWindowWidth();
    }

    private void RenderItemGroupPage(TCharConfig charConfig)
    {
        _itemGroupPage ??= CreateItemGroupPage();
        _itemGroupPage.RenderPage(charConfig);
    }

    private void RenderCharacterManagementPage(TCharConfig _)
    {
        _characterManagementPage ??= CreateCharacterManagementRenderer();
        _characterManagementPage.Draw();
    }

    private static void Tab(ReadOnlySpan<byte> name, TCharConfig characterConfig, Action<TCharConfig> contentSelector)
    {
        if (ImGui.BeginTabItem(name))
        {
            contentSelector(characterConfig);
            ImGui.EndTabItem();
        }
    }

    protected abstract void GeneralConfigTab(TCharConfig characterConfig);

    protected static void SelectItemGroup(TCharConfig config, ref string group, ReadOnlySpan<byte> label)
    {
        ControlHelper.SelectItem(
            config.Groups,
            x => x.Name,
            ref group,
            label,
            100);
    }

    protected static void SelectRouletteGroup(TCharConfig characterConfig, ReadOnlySpan<byte> label, ref string? groupName, ReadOnlySpan<byte> selectGroupLabel)
    {
        bool isEnabled = groupName is not null;

        _ = ImGui.Checkbox(label, ref isEnabled);

        if (isEnabled)
        {
            groupName ??= characterConfig.Groups.FirstOrDefault()?.Name;

            if (groupName is not null)
            {
                ImGui.SameLine();
                SelectItemGroup(characterConfig, ref groupName, selectGroupLabel);
            }
        }
        else
        {
            groupName = null;
        }
    }
}
