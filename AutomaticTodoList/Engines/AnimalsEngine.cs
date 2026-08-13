using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;

namespace AutomaticTodoList.Engines;

internal class AnimalsEngine(
    Action<string, StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled
) : BaseEngine<AnimalsTodoItem>(log, isEnabled, Frequency.OnceADay)
{
    public override void UpdateItems()
    {
        // group the animals by where they live (building/room) and add an item
        // for each location that still has unpetted, living members. this gives
        // "Barn: 2/5" instead of "Pig" etc.
        Farm? farm = Game1.getFarm();
        if (farm is null)
            return;

        var all = farm.getAllFarmAnimals();
        
        // build a dictionary location -> list of animals in that location
        var byLocation = all
            .Where(a => a != null && a.health.Value > 0)
            .GroupBy(AnimalsTodoItem.GroupKey, StringComparer.OrdinalIgnoreCase);

        foreach (var group in byLocation)
        {
            string location = group.Key;
            int total = group.Count();
            int unpetted = group.Count(a => !a.wasPet.Value);
            if (total > 0 && unpetted > 0)
                items.Add(new AnimalsTodoItem(location));
        }

    }
}
