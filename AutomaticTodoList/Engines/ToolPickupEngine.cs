using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;

namespace AutomaticTodoList.Engines;

internal class ToolPickupEngine(
    Action<string, StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled
) : BaseEngine<ToolPickupTodoItem>(log, isEnabled, Frequency.OnceADay)
{
    public override void UpdateItems()
    {
        var tool = Game1.player.toolBeingUpgraded.Value;
        if (tool is null)
            return;

        if (Game1.player.daysLeftForToolUpgrade.Value > 0)
            return;

        if (Utility.isFestivalDay())
            return;

        items.Add(new ToolPickupTodoItem(tool));
    }
}
