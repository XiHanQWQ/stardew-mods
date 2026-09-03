using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;
using StardewValley.Objects;

namespace AutomaticTodoList.Engines;

internal class HarvestableCrabPotsEngine(
    Action<string, StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled
) : BaseEngine<HarvestableCrabPotsTodoItem>(log, isEnabled, Frequency.EveryTimeChange)
{
    public override void UpdateItems()
    {
        var crabPotGroups = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        Utility.ForEachLocation(gameLocation =>
        {
            if (gameLocation is null)
                return true;

            int count = 0;
            foreach (var obj in gameLocation.objects.Values)
            {
                if (obj is CrabPot crabPot && crabPot.readyForHarvest.Value)
                    count++;
            }

            if (count > 0)
            {
                string displayName = gameLocation.DisplayName ?? gameLocation.Name;
                crabPotGroups.TryGetValue(displayName, out int existing);
                crabPotGroups[displayName] = existing + count;
            }

            return true;
        });

        foreach (var group in crabPotGroups)
        {
            if (group.Value > 0)
                items.Add(new HarvestableCrabPotsTodoItem(group.Key));
        }
    }
}
