using AutomaticTodoList.Models;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.SpecialOrders;

namespace AutomaticTodoList.Components.TodoItems;

/// <summary>A SpecialOrdersBoardTodoItem todo item.</summary>
/// <remarks>Initializes a new instance of the <see cref="SpecialOrdersBoardTodoItem"/> class.</remarks>
/// <param name="text">The text of the todo item.</param>
internal class SpecialOrdersBoardTodoItem(string orderType, bool isChecked = false)
    : BaseTodoItem(isChecked, TaskPriority.SpecialOrders)
{
    public string OrderType { get; } = orderType;

    public override string Text()
    {
        return SpecialOrderTypes.GetTextKey(this.OrderType) switch
        {
            "Standard" => I18n.Items_SpecialOrdersBoard_Standard_Text(),
            "Qi" => I18n.Items_SpecialOrdersBoard_Qi_Text(),
            _ => I18n.Items_SpecialOrdersBoard_Standard_Text()
        };
    }

    public override void OnOneSecondUpdateTicked(OneSecondUpdateTickedEventArgs e)
    {
        if (!IsChecked)
        {
            SpecialOrder leftOrder = Game1.player.team.GetAvailableSpecialOrder(0, this.OrderType);
            SpecialOrder rightOrder = Game1.player.team.GetAvailableSpecialOrder(1, this.OrderType);

            bool noOrderIsAvailable = leftOrder is null && rightOrder is null;
            bool alreadyAcceptedOrder = Game1.player.team.acceptedSpecialOrderTypes.Contains(this.OrderType);

            if (noOrderIsAvailable || alreadyAcceptedOrder)
            {
                this.MarkCompleted();
            }
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is SpecialOrdersBoardTodoItem otherItem && this.OrderType == otherItem.OrderType;
    }

    public override int GetHashCode()
    {
        return (this.GetType(), this.OrderType).GetHashCode();
    }
}
