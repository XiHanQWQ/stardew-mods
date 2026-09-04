using AutomaticTodoList.Models;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace AutomaticTodoList.Components.TodoItems;

internal class PetTodoItem(Pet pet, bool isChecked = false)
    : BaseTodoItem(isChecked, TaskPriority.Default)
{
    private readonly Pet pet = pet;

    public override string Text()
    {
        string name = this.pet.Name ?? "pet";
        return I18n.Items_Animals_Single_Text(name);
    }

    public override void OnOneSecondUpdateTicked(OneSecondUpdateTickedEventArgs e)
    {
        if (!IsChecked && HasBeenPetted(this.pet))
        {
            this.MarkCompleted();
        }
    }

    public static bool HasBeenPetted(Pet pet)
    {
        if (pet.lastPetDay.TryGetValue(Game1.player.UniqueMultiplayerID, out int lastDay))
        {
            return lastDay == Game1.Date.TotalDays;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        return obj is PetTodoItem other && this.pet == other.pet;
    }

    public override int GetHashCode()
    {
        return (this.GetType(), this.pet).GetHashCode();
    }
}
