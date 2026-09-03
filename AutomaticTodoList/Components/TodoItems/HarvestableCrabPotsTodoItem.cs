using AutomaticTodoList.Models;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace AutomaticTodoList.Components.TodoItems;

internal class HarvestableCrabPotsTodoItem(string groupName, bool isChecked = false)
    : BaseTodoItem(isChecked, TaskPriority.HarvestableCrops)
{
    public string GroupName { get; } = groupName;

    private int CrabPotCount { get; set; } = CountCrabPotsForGroup(groupName);

    public override string Text()
    {
        return I18n.Items_HarvestableCrabPots_Text(
            this.GroupName,
            this.CrabPotCount
        );
    }

    public override void OnTimeChanged(TimeChangedEventArgs e)
    {
        if (IsChecked)
        {
            var count = CountCrabPotsForGroup(this.GroupName);
            if (count != this.CrabPotCount)
            {
                this.CrabPotCount = count;

                if (this.CrabPotCount > 0)
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
            var count = CountCrabPotsForGroup(this.GroupName);
            if (count != this.CrabPotCount)
            {
                this.CrabPotCount = count;

                if (this.CrabPotCount == 0)
                {
                    this.MarkCompleted();
                }
            }
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is HarvestableCrabPotsTodoItem other &&
               string.Equals(this.GroupName, other.GroupName, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return (this.GetType(), this.GroupName.ToLowerInvariant()).GetHashCode();
    }

    internal static int CountCrabPotsForGroup(string groupName)
    {
        int count = 0;
        Utility.ForEachLocation(location =>
        {
            if (location is null)
                return true;

            string displayName = location.DisplayName ?? location.Name;
            if (string.Equals(displayName, groupName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var obj in location.objects.Values)
                {
                    if (obj is CrabPot crabPot && crabPot.readyForHarvest.Value)
                        count++;
                }
            }

            return true;
        });
        return count;
    }
}
