using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;

namespace AutomaticTodoList.Engines;

/// <summary>An engine which adds a single to-do for petting the player's house animal.</summary>
internal class PetEngine(
    Action<string,StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled
) : BaseEngine<PetTodoItem>(log, isEnabled, Frequency.OnceADay)
{
    public override void UpdateItems()
    {
        var pet = Game1.player.getPet();
        if (pet is null)
            return;

        if (!PetTodoItem.HasBeenPetted(pet))
            items.Add(new PetTodoItem());
    }
}