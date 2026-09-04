using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;

namespace AutomaticTodoList.Engines;

internal class QueenOfSauceEngine(
    Action<string, StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled
) : BaseEngine<QueenOfSauceTodoItem>(log, isEnabled, Frequency.OnceADay)
{
    private static readonly VirtualTV TV = new();
    private static readonly Dictionary<string, string> RecipesByDescription = new();

    public override void UpdateItems()
    {
        if (Game1.stats.DaysPlayed < 5)
            return;

        if (RecipesByDescription.Count == 0)
            LoadRecipes();

        string[] dialogue = TV.GetWeeklyRecipe();
        if (dialogue.Length == 0 || !RecipesByDescription.TryGetValue(dialogue[0], out string? recipeName))
            return;

        if (Game1.player.knowsRecipe(recipeName))
            return;

        var recipe = new CraftingRecipe(recipeName, true);
        items.Add(new QueenOfSauceTodoItem(recipe.DisplayName));
    }

    private static void LoadRecipes()
    {
        var cookingChannel = DataLoader.Tv_CookingChannel(Game1.temporaryContent);
        foreach (var entry in cookingChannel)
        {
            string[] fields = entry.Value.Split('/');
            if (fields.Length > 1)
            {
                RecipesByDescription[fields[1]] = fields[0];
            }
        }
    }
}
