using AutomaticTodoList.Models;
using StardewModdingAPI.Events;
using StardewValley;

namespace AutomaticTodoList.Components.TodoItems;

internal class ReadyMachinesTodoItem(string groupName, int initialCount = 0, bool isChecked = false)
    : BaseTodoItem(isChecked, TaskPriority.ReadyMachines)
{
    public string GroupName { get; } = groupName;

    /// <summary>The current count of ready machines in this group. Updated by the engine.</summary>
    public int ReadyMachinesCount { get; set; } = initialCount;

    public override string Text()
    {
        return I18n.Items_ReadyMachines_Text(
            this.GroupName,
            this.ReadyMachinesCount
        );
    }

    public override void OnTimeChanged(TimeChangedEventArgs e)
    {
        if (IsChecked && this.ReadyMachinesCount > 0)
        {
            this.MarkUncompleted();
        }
    }

    public override void OnOneSecondUpdateTicked(OneSecondUpdateTickedEventArgs e)
    {
        if (!IsChecked && this.ReadyMachinesCount == 0)
        {
            this.MarkCompleted();
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
}
