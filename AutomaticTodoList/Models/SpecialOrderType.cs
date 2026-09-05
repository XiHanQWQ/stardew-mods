using StardewValley;
using StardewValley.SpecialOrders;

namespace AutomaticTodoList.Models;

/// <summary>Helper methods for special order types using vanilla string constants.</summary>
internal static class SpecialOrderTypes
{
    /// <summary>Standard special orders (empty string type).</summary>
    public const string Standard = "";

    /// <summary>Qi special orders.</summary>
    public const string Qi = "Qi";

    /// <summary>All known order types to check.</summary>
    public static readonly string[] All = [Standard, Qi];

    /// <summary>Check if a special order board type is unlocked for the player.</summary>
    public static bool IsBoardUnlocked(string orderType)
    {
        return orderType switch
        {
            Standard => SpecialOrder.IsSpecialOrdersBoardUnlocked(),
            Qi => Math.Max(0, Game1.netWorldState.Value.GoldenWalnutsFound - 1) >= 100,
            _ => false
        };
    }

    /// <summary>Get the display text key for a special order type.</summary>
    public static string GetTextKey(string orderType)
    {
        return orderType switch
        {
            Standard => "Standard",
            Qi => "Qi",
            _ => orderType
        };
    }
}