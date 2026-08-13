using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace BerrySeasonReminder
{
    public class BillboardPatches
    {
        private const string BEARS_KNOWLEDGE_EVENT = "2120303";

        public static void performHoverAction_Postfix(ref Billboard __instance, int x, int y, ref string ___hoverText)
        {
            if (__instance.calendarDays != null &&
                (!ModEntry.Config.RequireBearsKnowledge || Game1.player.eventsSeen.Contains(BEARS_KNOWLEDGE_EVENT)))
            {
                for (int i = 0; i < __instance.calendarDays.Count; i++)
                {
                    ClickableTextureComponent c = __instance.calendarDays[i];
                    if (c.bounds.Contains(x, y))
                    {
                        if (Game1.currentSeason.Equals("fall") && i >= 7 && i <= 10)
                        {
                            if (___hoverText.Length > 0)
                            {
                                ___hoverText += Environment.NewLine;
                            }
                            ___hoverText += ModEntry.ModHelper.Translation.Get("blackberry.season");
                        }
                        if (Game1.currentSeason.Equals("spring") && i >= 14 && i <= 17)
                        {
                            if (___hoverText.Length > 0)
                            {
                                ___hoverText += Environment.NewLine;
                            }
                            ___hoverText += ModEntry.ModHelper.Translation.Get("salmonberry.season");
                        }
                    }
                }
            }
        }

        public static void draw_Postfix(ref Billboard __instance, SpriteBatch b, bool ___dailyQuestBoard, ref string ___hoverText)
        {
            if (!___dailyQuestBoard)
            {
                if (!ModEntry.Config.RequireBearsKnowledge || Game1.player.eventsSeen.Contains(BEARS_KNOWLEDGE_EVENT))
                {
                    if (Game1.currentSeason.Equals("fall"))
                    {
                        for (int i = 7; i <= 10; i++)
                        {
                            Utility.drawWithShadow(
                                b,
                                Game1.objectSpriteSheet,
                                new Vector2(__instance.calendarDays[i].bounds.X + 82,
                                            __instance.calendarDays[i].bounds.Y + 14 - Game1.dialogueButtonScale / 2f),
                                new Rectangle(32, 272, 16, 16),
                                Color.White, 0f, Vector2.Zero, 2f, false, 1f, -1, -1, 0.35f);
                        }
                    }
                    else if (Game1.currentSeason.Equals("spring"))
                    {
                        for (int j = 14; j <= 17; j++)
                        {
                            Utility.drawWithShadow(
                                b,
                                Game1.objectSpriteSheet,
                                new Vector2(__instance.calendarDays[j].bounds.X + 82,
                                            __instance.calendarDays[j].bounds.Y + 14 - Game1.dialogueButtonScale / 2f),
                                new Rectangle(128, 192, 16, 16),
                                Color.White, 0f, Vector2.Zero, 2f, false, 1f, -1, -1, 0.35f);
                        }
                    }

                    Game1.mouseCursorTransparency = 1f;
                    __instance.drawMouse(b, false, -1);

                    if (___hoverText.Length > 0)
                    {
                        IClickableMenu.drawHoverText(
                            b, ___hoverText, Game1.dialogueFont, 0, 0, -1, null, -1, null, null, 0, null, -1, -1, -1,
                            1f, null, null, null, null, null, null, 1f, -1, -1);
                    }
                }
            }
        }
    }
}