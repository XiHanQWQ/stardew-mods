using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;
using System.Collections.Generic;

namespace AutomaticTodoList.Engines;

internal class ReadyMachinesEngine(
    Action<string, StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled
) : BaseEngine<ReadyMachinesTodoItem>(log, isEnabled, Frequency.EverySecond)
{
    private readonly Dictionary<string, int> cachedGroupCounts = new(StringComparer.OrdinalIgnoreCase);

    public override void UpdateItems()
    {
        var machineGroups = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        Utility.ForEachLocation(gameLocation =>
        {
            if (gameLocation is null)
                return true;

            int count = gameLocation.GetNumberOfReadyMachinesExcludingBuildings();
            if (count > 0)
            {
                string displayName = gameLocation.GetLocationDisplayName();
                machineGroups.TryGetValue(displayName, out int existing);
                machineGroups[displayName] = existing + count;
            }

            return true;
        });

        // Update cached counts
        cachedGroupCounts.Clear();
        foreach (var group in machineGroups)
        {
            if (group.Value > 0)
            {
                cachedGroupCounts[group.Key] = group.Value;
            }
        }

        // Update or create todo items
        foreach (var group in machineGroups)
        {
            if (group.Value > 0)
            {
                // Find existing item or create new
                var existingItem = items.FirstOrDefault(i => string.Equals(i.GroupName, group.Key, StringComparison.OrdinalIgnoreCase));
                if (existingItem != null)
                {
                    existingItem.ReadyMachinesCount = group.Value;
                }
                else
                {
                    items.Add(new ReadyMachinesTodoItem(group.Key, group.Value));
                }
            }
        }

        // Remove items for groups that no longer have ready machines
        var itemsToRemove = new List<ReadyMachinesTodoItem>();
        foreach (ReadyMachinesTodoItem item in items)
        {
            if (!cachedGroupCounts.ContainsKey(item.GroupName))
            {
                itemsToRemove.Add(item);
            }
        }
        foreach (var item in itemsToRemove)
        {
            items.Remove(item);
        }
    }

    /// <summary>Get the cached ready machine count for a group.</summary>
    public int GetCachedCount(string groupName)
    {
        return cachedGroupCounts.TryGetValue(groupName, out int count) ? count : 0;
    }
}
