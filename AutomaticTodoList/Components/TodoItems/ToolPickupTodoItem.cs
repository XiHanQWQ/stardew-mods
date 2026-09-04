using AutomaticTodoList.Models;
using StardewModdingAPI.Events;
using StardewValley;

namespace AutomaticTodoList.Components.TodoItems;

internal class ToolPickupTodoItem(Tool tool, bool isChecked = false)
    : BaseTodoItem(isChecked, TaskPriority.ToolPickup)
{
    private Item ReadyTool { get; } = tool;

    public override string Text()
    {
        return I18n.Items_ToolPickup_Text(this.ReadyTool.DisplayName);
    }

    public override void OnOneSecondUpdateTicked(OneSecondUpdateTickedEventArgs e)
    {
        if (!IsChecked && Game1.player.toolBeingUpgraded.Value is null)
        {
            this.MarkCompleted();
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is ToolPickupTodoItem otherItem && this.ReadyTool.Name == otherItem.ReadyTool.Name;
    }

    public override int GetHashCode()
    {
        return (this.GetType(), this.ReadyTool.Name).GetHashCode();
    }
}
