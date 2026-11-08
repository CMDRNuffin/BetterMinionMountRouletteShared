namespace BetterRouletteBase.UI.Base;

using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using DisabledDisposable = Dalamud.Interface.Utility.Raii.ImRaii.DisabledDisposable;

internal abstract class DialogWindow : Window
{
    protected DialogWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(name, flags, forceMainWindow)
    {
        AllowClickthrough = false;
        AllowPinning = false;
    }

    public new bool ShowCloseButton
    {
        get => base.ShowCloseButton;
        set
        {
            base.ShowCloseButton = value;
            base.RespectCloseHotkey &= value;
        }
    }

    public new bool RespectCloseHotkey
    {
        get => base.RespectCloseHotkey;
        set
        {
            base.RespectCloseHotkey = value;
            base.ShowCloseButton |= value;
        }
    }

    public bool IsEnabled { get; set; } = true;

    public sealed override void Draw()
    {
        using DisabledDisposable _ = ImRaii.Disabled(!IsEnabled);
        DrawImpl();
    }

    protected abstract void DrawImpl();
}
