namespace BetterRouletteBase.UI;

using BetterRouletteBase.UI.Base;

using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin;

internal abstract class WindowManagerBase : IDisposable
{
    private bool _disposedValue;
    private readonly IDalamudPluginInterface _dalamudPluginInterface;

    protected WindowManagerBase(IDalamudPluginInterface dalamudPluginInterface)
    {
        dalamudPluginInterface.UiBuilder.Draw += Draw;
        dalamudPluginInterface.UiBuilder.OpenConfigUi += OpenConfigWindow;
        _dalamudPluginInterface = dalamudPluginInterface;
    }

    protected WindowStack InternalWindows { get; } = new();

    protected WindowStack InternalDialogs { get; } = new();

    public void Draw()
    {
        ImGui.BeginDisabled(InternalDialogs.HasWindows);
        InternalWindows.Draw();
        ImGui.EndDisabled();

        InternalDialogs.Draw();
    }

    public void Add(Window window)
    {
        InternalWindows.Add(window);
    }

    public void RemoveWindow(Window window)
    {
        RemoveWindowInternal(InternalWindows, window);
    }

    public void RemoveDialog(Window window)
    {
        RemoveWindowInternal(InternalDialogs, window);
    }

    private static void RemoveWindowInternal(WindowStack system, Window window)
    {
        system.Remove(window);
    }

    public void OpenDialog(DialogWindow window)
    {
        window.IsOpen = true;
        InternalDialogs.Add(window);
    }

    protected abstract Window GetOrCreateConfigWindow(out bool isNew);

    public void OpenConfigWindow()
    {
        Window configWindow = GetOrCreateConfigWindow(out bool isNew);
        configWindow.IsOpen = true;
        if (isNew)
        {
            Add(configWindow);
        }
        else
        {
            configWindow.BringToFront();
        }
    }

    public void Confirm(string title, string text, params ButtonConfig[] buttons)
    {
        OpenDialog(new DialogPrompt(title, text, buttons));
    }

    public void ConfirmYesNo(string title, string text, Action confirmed)
    {
        Confirm(title, text, ("Yes", confirmed), "No");
    }

    public readonly struct ButtonConfig
    {
        public readonly string Text;
        public readonly Action? Execute;

        private ButtonConfig(string text)
        {
            Text = text;
            Execute = null;
        }

        private ButtonConfig(string text, Action execute)
        {
            Text = text;
            Execute = execute;
        }

        public static implicit operator ButtonConfig(string text)
        {
            return new ButtonConfig(text);
        }

        public static implicit operator ButtonConfig((string text, Action execute) value)
        {
            return new(value.text, value.execute);
        }

        public static implicit operator ButtonConfig((Action execute, string text) value)
        {
            return new(value.text, value.execute);
        }
    }

    protected sealed class WindowStack
    {
        private readonly List<IWindow> _windowsToRemove = new();
        private readonly WindowSystem _windows = new();

        public bool HasWindows => _windows.Windows.Count > 0;

        public IReadOnlyList<IWindow> Windows => _windows.Windows;

        public void Draw()
        {
            // clean up closed windows
            // maybe todo: keep windows that we want to keep alive?
            foreach (IWindow window in _windowsToRemove)
            {
                Remove(window);
            }

            _windowsToRemove.Clear();
            _windowsToRemove.AddRange(_windows.Windows.Where(x => !x.IsOpen));
            _windows.Draw();
        }

        public void Remove(IWindow window)
        {
            if (_windows.Windows.Contains(window))
            {
                // if window is still open, close it so any close action can happen
                if (window.IsOpen)
                {
                    window.IsOpen = false;
                }
                else
                {
                    _windows.RemoveWindow(window);
                    if (window is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        public void Add(IWindow window)
        {
            _windows.AddWindow(window);
        }
    }

    protected virtual void DisposeInternal(bool disposing)
    {
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            DisposeInternal(disposing);
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            _dalamudPluginInterface.UiBuilder.Draw -= Draw;
            _dalamudPluginInterface.UiBuilder.OpenConfigUi -= OpenConfigWindow;

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~WindowManagerBase()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
