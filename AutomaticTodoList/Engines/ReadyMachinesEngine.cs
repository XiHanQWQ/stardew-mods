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

            // count machines in this top-level location
            int count = gameLocation.GetNumberOfReadyMachinesExcludingBuildings();
            if (count > 0)
            {
                string displayName = gameLocation.DisplayName ?? gameLocation.Name;
                machineGroups.TryGetValue(displayName, out int existing);
                machineGroups[displayName] = existing + count;
            }

            // count machines in buildings at this location
            foreach (var building in gameLocation.buildings)
            {
                if (building?.indoors?.Value is GameLocation interior)
                {
                    int buildingCount = interior.GetNumberOfReadyMachinesExcludingBuildings();
                    if (buildingCount > 0)
                    {
                        string buildingName = ReadyMachinesTodoItem.GetBuildingDisplayName(building);
                        machineGroups.TryGetValue(buildingName, out int existing);
                        machineGroups[buildingName] = existing + buildingCount;
                    }
                }
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
