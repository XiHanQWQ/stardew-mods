using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;

namespace AutomaticTodoList.Engines;

internal class ReadyMachinesEngine(
    Action<string, StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled
) : BaseEngine<ReadyMachinesTodoItem>(log, isEnabled, Frequency.EveryTimeChange)
{
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

        foreach (var group in machineGroups)
        {
            if (group.Value > 0)
                items.Add(new ReadyMachinesTodoItem(group.Key));
        }
    }
}
