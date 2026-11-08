namespace BetterRouletteBase.UI;

using Dalamud.Interface.Utility;

using Dalamud.Bindings.ImGui;

using System;
using System.Collections.Generic;

using Dalamud.Interface.Utility.Raii;
using ComboDisposable = Dalamud.Interface.Utility.Raii.ImRaii.ComboDisposable;

internal static class ControlHelper
{
    /// <summary>
    /// Works for disabled elements. Use <see cref="Tooltip(bool, ReadOnlySpan{byte})"/> if that is not desired.
    /// </summary>
    /// <param name="text">The text of the tooltip.</param>
    public static void Tooltip(ReadOnlySpan<byte> text)
    {
        Tooltip(true, text);
    }

    public static void Tooltip(bool allowDisabled, ReadOnlySpan<byte> text)
    {
        using ConditionalTooltipDisposable tooltip = Tooltip(allowDisabled);
        if (tooltip.Success)
        {
            ImGui.Text(text);
        }
    }

    public static ConditionalTooltipDisposable Tooltip(bool allowDisabled = true)
    {
        ImGuiHoveredFlags flags = allowDisabled
            ? ImGuiHoveredFlags.AllowWhenDisabled
            : ImGuiHoveredFlags.None;
        return new(ImGui.IsItemHovered(flags));
    }

    public static void SelectItem<T>(IEnumerable<T> items, Func<T, string> nameSelector, ref string selectedName, ReadOnlySpan<byte> label, float? width = null)
    {
        if (width is float w)
        {
            ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * w);
        }

        using ComboDisposable combo = ImRaii.Combo(label, selectedName);
        if (combo.Success)
        {
            foreach (T item in items)
            {
                string name = nameSelector(item);
                if (ImGui.Selectable(name, name == selectedName))
                {
                    selectedName = name;
                }
            }
        }
    }

    public static byte[] Concat(ReadOnlySpan<byte> part1, ReadOnlySpan<byte> part2, ReadOnlySpan<byte> part3)
    {
        byte[] res = new byte[part1.Length + part2.Length + part3.Length];
        part1.CopyTo(res.AsSpan());
        part2.CopyTo(res.AsSpan(part1.Length));
        part3.CopyTo(res.AsSpan(part1.Length + part2.Length));
        return res;
    }

    public ref struct ConditionalTooltipDisposable : IDisposable
    {
        public ConditionalTooltipDisposable(bool activate)
        {
            if (activate)
            {
                ImGui.BeginTooltip();
            }

            Success = activate;
        }

        public bool Success { get; private set; }

        public void Dispose()
        {
            if (!Success)
            {
                return;
            }

            Success = false;
            ImGui.EndTooltip();
        }
    }
}
