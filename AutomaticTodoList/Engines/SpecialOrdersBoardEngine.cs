using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;
using StardewValley.SpecialOrders;

namespace AutomaticTodoList.Engines;

internal class SpecialOrdersBoardEngine(
    Action<string, StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled
) : BaseEngine<SpecialOrdersBoardTodoItem>(log, isEnabled, Frequency.OnceADay)
{
    public override void UpdateItems()
    {
        foreach (string orderType in SpecialOrderTypes.All)
        {
            if (!SpecialOrderTypes.IsBoardUnlocked(orderType))
            {
                continue;
            }

            SpecialOrder leftOrder = Game1.player.team.GetAvailableSpecialOrder(0, orderType);
            SpecialOrder rightOrder = Game1.player.team.GetAvailableSpecialOrder(1, orderType);

            bool anyOrderIsAvailable = leftOrder is not null || rightOrder is not null;
            bool alreadyAcceptedOrder = Game1.player.team.acceptedSpecialOrderTypes.Contains(orderType);

            if (anyOrderIsAvailable && !alreadyAcceptedOrder)
            {
                items.Add(new SpecialOrdersBoardTodoItem(orderType));
            }
        }
    }
}
