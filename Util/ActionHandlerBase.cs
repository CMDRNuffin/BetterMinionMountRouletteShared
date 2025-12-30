namespace BetterRouletteBase.Util;

using Dalamud.Hooking;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using System;

internal abstract class ActionHandlerBase : IDisposable
{
    private bool _disposedValue;
    private readonly Hook<ActionManager.Delegates.UseAction> _useActionDetour = null!;

    protected unsafe ActionHandlerBase(IGameInteropProvider gameInteropProvider, IPluginLog pluginLog)
    {
        PluginLog = pluginLog;

        void* renderAddress = ActionManager.MemberFunctionPointers.UseAction;
        if (renderAddress is null)
        {
            pluginLog.Debug("Unable to load UseAction address");
            return;
        }

        _useActionDetour = gameInteropProvider.HookFromAddress<ActionManager.Delegates.UseAction>(renderAddress, UseActionDetour);
        _useActionDetour.Enable();
    }

    protected IPluginLog PluginLog { get; }

    protected abstract bool OnUseAction(UseActionArgs args);

    private unsafe bool UseActionDetour(
        ActionManager* actionManager,
        ActionType actionType,
        uint actionID,
        ulong targetID,
        uint extraParam,
        ActionManager.UseActionMode
        useActionMode,
        uint comboRouteId,
        bool* outOptAreaTargeted)
    {
        return OnUseAction(new(
            _useActionDetour,
            actionManager,
            actionType,
            actionID,
            targetID,
            extraParam,
            useActionMode,
            comboRouteId,
            outOptAreaTargeted));
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

            _useActionDetour?.Dispose();

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    ~ActionHandlerBase()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected unsafe ref struct UseActionArgs(
        Hook<ActionManager.Delegates.UseAction> hook,
        Pointer<ActionManager> actionManager,
        ActionType actionType,
        uint actionID,
        ulong targetID,
        uint extraParam,
        ActionManager.UseActionMode useActionMode,
        uint comboRouteId,
        Pointer<bool> outOptAreaTargeted)
    {
        private readonly Hook<ActionManager.Delegates.UseAction> _hook = hook;
        public Pointer<ActionManager> ActionManager = actionManager;
        public ActionType ActionType = actionType;
        public uint ActionID = actionID;
        public ulong TargetID = targetID;
        public uint ExtraParam = extraParam;
        public ActionManager.UseActionMode UseActionMode = useActionMode;
        public uint ComboRouteID = comboRouteId;
        public Pointer<bool> OutOptAreaTargeted = outOptAreaTargeted;

        public bool Original()
        {
            return _hook.Original(ActionManager, ActionType, ActionID, TargetID, ExtraParam, UseActionMode, ComboRouteID, OutOptAreaTargeted);
        }
    }
}
