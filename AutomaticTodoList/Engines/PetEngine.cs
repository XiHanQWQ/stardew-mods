using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;
using StardewValley.Characters;

namespace AutomaticTodoList.Engines;

internal class PetEngine(
    Action<string, StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled
) : BaseEngine<PetTodoItem>(log, isEnabled, Frequency.OnceADay)
{
    public override void UpdateItems()
    {
        Farm? farm = Game1.getFarm();
        if (farm is null)
            return;

        foreach (var character in farm.characters)
        {
            if (character is Pet pet && !PetTodoItem.HasBeenPetted(pet))
            {
                items.Add(new PetTodoItem(pet));
            }
        }
    }
}
