using System.Reflection;
using AutomaticTodoList.Models;
using StardewModdingAPI.Events;
using StardewValley;

namespace AutomaticTodoList.Components.TodoItems;


internal class AnimalsTodoItem(string group, bool isChecked = false)
    : BaseTodoItem(isChecked, TaskPriority.Default)
{
    // each todo tracks progress for a given animal group (previously type). this
    // might now represent the location where the animals live (e.g. "Barn"). we
    // compute the same key when running the query so the counts stay in sync.

    public string Group { get; } = group;

    public override string Text()
    {
        Farm? farm = Game1.getFarm();
        if (farm == null)
            return string.Empty;

        var all = farm.getAllFarmAnimals();
        var matching = all.Where(a => a != null &&
                                     GroupKey(a).Equals(this.Group, StringComparison.OrdinalIgnoreCase));
        int total = matching.Count(a => a.health.Value > 0);
        int petted = matching.Count(a => a.health.Value > 0 &&
                                         (a.wasPet.Value || a.health.Value <= 0));
        return I18n.Items_Animals_ByType_Text(this.Group, total-petted);
    }

    public override void OnOneSecondUpdateTicked(OneSecondUpdateTickedEventArgs e)
    {
        if (!IsChecked)
        {
            Farm? farm = Game1.getFarm();
            if (farm == null)
                return;

            bool allDone = farm.getAllFarmAnimals()
                .Where(a => a != null &&
                            GroupKey(a).Equals(this.Group, StringComparison.OrdinalIgnoreCase))
                .All(a => a == null || a.health.Value <= 0 || a.wasPet.Value);
            if (allDone)
                this.MarkCompleted();
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is AnimalsTodoItem other &&
               string.Equals(this.Group, other.Group, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Compute the grouping key for an animal (used by both engine and item).</summary>
    public static string GroupKey(FarmAnimal a)
    {
        if (a is null)
            return string.Empty;

        // prefer the building's displayName if the animal has a home
        if (a.home is StardewValley.Buildings.Building b)
        {
            // use reflection because the field/property isn't public in the SDK
            var nameProp = b.GetType().GetProperty("displayName")
                           ?? b.GetType().GetProperty("DisplayName");
            if (nameProp != null && nameProp.GetValue(b) is string s && !string.IsNullOrEmpty(s))
                return s;
            var nameField = b.GetType().GetField("displayName");
            if (nameField != null && nameField.GetValue(b) is string sf && !string.IsNullOrEmpty(sf))
                return sf;
        }

        // fall back to the animal's "displayHouse" field if present
        var field = a.GetType().GetField("displayHouse");
        if (field != null && field.GetValue(a) is string dh && !string.IsNullOrEmpty(dh))
            return dh;

        // last resort, use the current location name
        return a.currentLocation?.Name ?? string.Empty;
    }

    public override int GetHashCode()
    {
        return (this.GetType(), this.Group.ToLowerInvariant()).GetHashCode();
    }
}
