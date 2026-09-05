using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;

namespace AutomaticTodoList.Engines;

internal class WaterableCropsEngine(
    Action<string, StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled
) : BaseEngine<WaterableCropsTodoItem>(log, isEnabled, Frequency.EverySecond)
{
    public override void UpdateItems()
    {
        Utility.ForEachLocation(gameLocation =>
        {
            if (gameLocation is null)
                return true;

            int waterableCount = gameLocation.GetTotalUnwateredCropsExcludingGinger();
            if (waterableCount > 0)
            {
                items.Add(new WaterableCropsTodoItem(gameLocation));
            }

            return true;
        });
    }
}
