using AutomaticTodoList.Models;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.TokenizableStrings;

namespace AutomaticTodoList.Components.TodoItems;

internal class ReadyMachinesTodoItem(string groupName, bool isChecked = false)
    : BaseTodoItem(isChecked, TaskPriority.ReadyMachines)
{
    public string GroupName { get; } = groupName;

    private int ReadyMachinesCount { get; set; } = CountMachinesForGroup(groupName);

    public override string Text()
    {
        return I18n.Items_ReadyMachines_Text(
            this.GroupName,
            this.ReadyMachinesCount
        );
    }

    public override void OnTimeChanged(TimeChangedEventArgs e)
    {
        if (IsChecked)
        {
            var machineCount = CountMachinesForGroup(this.GroupName);
            if (machineCount != this.ReadyMachinesCount)
            {
                this.ReadyMachinesCount = machineCount;

                if (this.ReadyMachinesCount > 0)
                {
                    this.MarkUncompleted();
                }
            }
        }
    }

    public override void OnOneSecondUpdateTicked(OneSecondUpdateTickedEventArgs e)
    {
        if (!IsChecked)
        {
            var machineCount = CountMachinesForGroup(this.GroupName);
            if (machineCount != this.ReadyMachinesCount)
            {
                this.ReadyMachinesCount = machineCount;

                if (this.ReadyMachinesCount == 0)
                {
                    this.MarkCompleted();
                }
            }
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is ReadyMachinesTodoItem other &&
               string.Equals(this.GroupName, other.GroupName, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return (this.GetType(), this.GroupName.ToLowerInvariant()).GetHashCode();
    }

    internal static int CountMachinesForGroup(string groupName)
    {
        int count = 0;
        Utility.ForEachLocation(location =>
        {
            if (location is null)
                return true;

            string displayName = location.DisplayName ?? location.Name;
            if (string.Equals(displayName, groupName, StringComparison.OrdinalIgnoreCase))
            {
                count += location.GetNumberOfReadyMachinesExcludingBuildings();
            }

            foreach (var building in location.buildings)
            {
                if (building?.indoors?.Value is GameLocation interior)
                {
                    string buildingName = GetBuildingDisplayName(building);
                    if (string.Equals(buildingName, groupName, StringComparison.OrdinalIgnoreCase))
                    {
                        count += interior.GetNumberOfReadyMachinesExcludingBuildings();
                    }
                }
            }

            return true;
        });
        return count;
    }

    internal static string GetBuildingDisplayName(StardewValley.Buildings.Building building)
    {
        if (building.GetData() is BuildingData data && !string.IsNullOrEmpty(data.Name))
            return TokenParser.ParseText(data.Name);

        string buildingType = building.buildingType.Value;
        if (!string.IsNullOrEmpty(buildingType))
        {
            string localized = Game1.content.LoadStringReturnNullIfNotFound(
                $"Strings/Buildings:{buildingType}_name"
            );
            if (!string.IsNullOrEmpty(localized))
                return localized;
        }

        return buildingType;
    }
}
