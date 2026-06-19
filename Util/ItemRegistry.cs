namespace BetterRouletteBase.Util;

using BetterRouletteBase.Config;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Responsible for maintaining a list of mounts with ID, name, icon, and whether or not the mount is unlocked.
/// </summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection")]
internal abstract class ItemRegistry<TItem, TGroup> where TItem : ItemData
    where TGroup : IItemGroup
{
    private readonly IClientState _clientState;
    private readonly Dictionary<uint, TItem> _itemsByID = [];
    private readonly List<TItem> _unlockedItems = [];
    private readonly List<TItem> _availableItems = [];

    private bool _isInitialized;
    private readonly object _lock = new();

    public ItemRegistry(IClientState clientState)
    {
        _clientState = clientState;
    }

    public int UnlockedItemCount { get; private set; }

    protected List<TItem> InternalItems { get; } = [];

    protected void InitializeIfNecessary()
    {
        // make sure initialization only runs once
        if (_isInitialized)
        {
            return;
        }

        lock (_lock)
        {
            // make sure initialization only runs once
            // (again, in case multiple threads called this at the same time)
            if (_isInitialized)
            {
                return;
            }

            InternalItems.AddRange(GetAllItems());
            foreach (TItem item in InternalItems)
            {
                _itemsByID.Add(item.ID, item);
            }

            _isInitialized = true;
        }
    }

    protected abstract bool IsItemUnlocked(uint id);

    public void RefreshUnlocked()
    {
        if (!_clientState.IsLoggedIn)
        {
            return;
        }

        InitializeIfNecessary();
        int count = 0;
        _unlockedItems.Clear();
        foreach (TItem item in InternalItems)
        {
            if (item.Unlocked = IsItemUnlocked(item.ID))
            {
                _unlockedItems.Add(item);
                ++count;
            }
        }

        UnlockedItemCount = count;
    }

    protected abstract IEnumerable<TItem> GetAllItems();

    public List<TItem> GetUnlockedItems()
    {
        InitializeIfNecessary();
        return _unlockedItems;
    }

    protected virtual void GatherExtraItemData(TItem item) { }

    protected virtual List<TItem> FilterAvailableItems(List<TItem> items, TGroup group) { return items; }

    private List<TItem> GetAvailableMounts(Pointer<ActionManager> actionManager, TGroup group, uint except)
    {
        RefreshUnlocked();
        List<TItem> unlockedItems = GetUnlockedItems();
        _availableItems.Clear();
        List<TItem> result = _availableItems;

        foreach (TItem item in unlockedItems)
        {
            if (group.IncludedItems.Contains(item.ID) == group.IncludedMeansActive
                && item.IsAvailable(actionManager)
                && item.ID != except)
            {
                result.Add(item);
                GatherExtraItemData(item);
            }
        }

        return FilterAvailableItems(result, group);
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Non-critical use of randomness, so we prefer speed over security")]
    public virtual uint GetRandom(Pointer<ActionManager> actionManager, TGroup group, uint except = 0)
    {
        List<TItem> available = GetAvailableMounts(actionManager, group, except);

        if (available.Count is 0)
        {
            // shortcut: no active items, can't select anything
            return 0;
        }

        if (available.Count is 1)
        {
            // shortcut: exactly one active item: can only select that, no matter what
            return available[0].ID;
        }

        int index = Random.Shared.Next(available.Count);
        return available[index].ID;
    }
}
