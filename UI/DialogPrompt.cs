namespace BetterRouletteBase.UI;

using BetterRouletteBase.UI.Base;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

internal sealed class DialogPrompt : DialogWindow
{
    private readonly string[] _lines;
    private readonly float[] _widths;
    private float _largestWidth;
    private float _lastScale;
    private readonly WindowManagerBase.ButtonConfig[] _buttons;

    public DialogPrompt(string title, string text, WindowManagerBase.ButtonConfig[] buttons)
        : base(title, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings)
    {
        _lines = text.Split('\n');
        _widths = new float[_lines.Length];
        _buttons = buttons;

        for (int i = 0; i < _lines.Length; ++i)
        {
            _lines[i] = _lines[i].Trim();
        }
    }

    protected override void DrawImpl()
    {
        // step 1: reserve width so stuff like
        //   > short line
        //   > very long line that stretches the window beyond what the title bar requires
        // doesn't end up looking exactly like here. Even though we probably won't ever need it.
        //
        // maybe idea for the future to also allow centering when the buttons are longer than the text:
        //   render buttons before text (store cursor pos, reserve entire text rectangle instead of width,
        //   draw buttons, restore cursor, draw text)
        if (_lastScale == ImGuiHelpers.GlobalScale)
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                float width = ImGui.CalcTextSize(_lines[i]).X + 10;
                _widths[i] = width;
                if (width > _largestWidth)
                {
                    _largestWidth = width;
                }
            }

            // ensure the init code doesn't run again
            _lastScale = ImGuiHelpers.GlobalScale;
        }

        ImGui.Dummy(new(_largestWidth, 0));

        // step 2: actually draw the lines
        for (int i = 0; i < _lines.Length; i++)
        {
            CenterText(_widths[i]);
            ImGui.Text(_lines[i]);
        }

        bool hasButton = false;
        foreach (WindowManagerBase.ButtonConfig button in _buttons)
        {
            if (hasButton)
            {
                ImGui.SameLine();
            }

            hasButton = true;
            if (ImGui.Button(button.Text))
            {
                IsOpen = false;
                button.Execute?.Invoke();
            }
        }
    }

    private static void CenterText(float textWidth)
    {
        float windowWidth = ImGui.GetWindowSize().X;

        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
    }
}
