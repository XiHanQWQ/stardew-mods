using System;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;

namespace BerrySeasonReminder
{
    public class ModEntry : Mod
    {
        public static IModHelper ModHelper { get; private set; } = null!;
        public static BerrySeasonReminderConfig Config { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            Config = helper.ReadConfig<BerrySeasonReminderConfig>();

            // 订阅游戏启动事件，用于注册 GMCM
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

            Harmony harmony = new Harmony(ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Billboard), "draw", new Type[] { typeof(SpriteBatch) }),
                postfix: new HarmonyMethod(typeof(BillboardPatches), nameof(BillboardPatches.draw_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Billboard), "performHoverAction"),
                postfix: new HarmonyMethod(typeof(BillboardPatches), nameof(BillboardPatches.performHoverAction_Postfix))
            );
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            GenericModConfigMenuIntegration.Register(
                mod: this.ModManifest,
                helper: this.Helper,
                getConfig: () => Config,
                setConfig: config => Config = config
            );
        }
    }
}