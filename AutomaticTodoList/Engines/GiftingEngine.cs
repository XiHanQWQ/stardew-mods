using AutomaticTodoList.Components.TodoItems;
using AutomaticTodoList.Models;
using StardewValley;
using System.Collections.Generic;

namespace AutomaticTodoList.Engines;

internal class GiftingEngine(
    Action<string, StardewModdingAPI.LogLevel> log,
    Func<bool> isEnabled,
    Func<string> enabledNPCsString
) : BaseEngine<GiftingTodoItem>(log, isEnabled, Frequency.OnceADay)
{
    private List<string>? cachedEnabledNPCs = null;
    private string? lastEnabledNPCsString = null;

    public override IEnumerable<ITodoItem> Items()
    {
        return this.items.Where(item => IsEnabledForNPC(item.NPC.Name));
    }

    public override void UpdateItems()
    {
        // Update cache if the config string changed
        string currentString = enabledNPCsString();
        if (currentString != lastEnabledNPCsString)
        {
            lastEnabledNPCsString = currentString;
            cachedEnabledNPCs = ParseEnabledNPCs(currentString);
        }

        // check if we still need to give gifts out for NPCs
        Utility.ForEachCharacter((npc) =>
        {
            if (npc.CanReceiveGifts() && Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship) && friendship.GiftsThisWeek < 2)
            {
                WeeklyGiftOrdinal ordinal = friendship.GiftsThisWeek == 0 ? WeeklyGiftOrdinal.First : WeeklyGiftOrdinal.Second;

                items.Add(new GiftingTodoItem(npc, friendship, ordinal));
            }

            return true;
        });
    }

    private List<string> ParseEnabledNPCs(string npcString)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(npcString))
            return result;

        foreach (var part in npcString.Split(','))
        {
            string trimmed = part.Trim().ToLower();
            if (!string.IsNullOrEmpty(trimmed))
                result.Add(trimmed);
        }
        return result;
    }

    private bool IsEnabledForNPC(string npcName)
    {
        if (cachedEnabledNPCs == null || cachedEnabledNPCs.Count == 0)
            return true; // empty list means all NPCs enabled

        return cachedEnabledNPCs.Contains(npcName.ToLower());
    }
}
