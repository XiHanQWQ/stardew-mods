using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocationsPerformancePatch
{
    public class ModEntry : Mod
    {
        public static IMonitor? ModMonitor;
        public static bool IsMapOpen { get; private set; }

        // Message IDs, namespaced so they can't collide with other mods
        private const string MsgPrefix = "NPCMapLocationsPerformancePatch";
        private const string RequestSyncId = MsgPrefix + ".RequestSync";
        private const string StopSyncId = MsgPrefix + ".StopSync";

        // Cached once; avoids re-acquiring the reflection field on every check
        private static readonly FieldInfo? PagesField = typeof(GameMenu).GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic);

        private long? cachedHostId;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;

            try
            {
                PatchNpcMapLocations(new Harmony(ModManifest.UniqueID));
                Monitor.Log("NPC Map Locations Performance Patch loaded!", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Init error: {ex}", LogLevel.Error);
            }

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
            helper.Events.Multiplayer.PeerDisconnected += OnPeerDisconnected;
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        }

        /// <summary>Patches NPC Map Locations' per-tick update handler. If the target method can't be found, NML simply keeps its vanilla behavior.</summary>
        private void PatchNpcMapLocations(Harmony harmony)
        {
            var target = AccessTools.TypeByName("NPCMapLocations.ModEntry");
            if (target == null)
            {
                Monitor.Log("NPCMapLocations.ModEntry type not found; patch not applied.", LogLevel.Error);
                return;
            }

            var method = AccessTools.Method(target, "OnUpdateTicked")
                ?? AccessTools.Method(target, "GameLoop_UpdateTicked")
                ?? AccessTools.Method(target, "UpdateTicked");
            if (method == null)
            {
                Monitor.Log("Could not find UpdateTicked method to patch; patch not applied.", LogLevel.Warn);
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(AccessTools.Method(typeof(NPCMapLocationsPatch), nameof(NPCMapLocationsPatch.UpdatePrefix)))
            );
            Monitor.Log($"Patched {target.Name}.{method.Name}", LogLevel.Debug);
        }

        /// <summary>Polls the active menu each tick (a few reference checks) instead of listening to MenuChanged, which does not fire when the player switches to the map tab inside an already-open GameMenu.</summary>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            SetMapOpen(IsMapMenuOpen());
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            IsMapOpen = false;
            cachedHostId = null;
            NPCMapLocationsPatch.Reset();
        }

        private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
        {
            if (e.Peer.IsHost)
            {
                cachedHostId = e.Peer.PlayerID;
                Monitor.Log($"Host connected: {e.Peer.PlayerID}", LogLevel.Debug);
            }

            // If we are the host, clean up any leftover requests for this client (shouldn't happen)
            if (Context.IsMainPlayer)
            {
                NPCMapLocationsPatch.OnClientDisconnected(e.Peer.PlayerID);
            }
        }

        private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
        {
            if (e.Peer.IsHost)
            {
                cachedHostId = null;
                Monitor.Log("Host disconnected", LogLevel.Debug);
            }

            // If we are the host, remove this client's sync requests
            if (Context.IsMainPlayer)
            {
                NPCMapLocationsPatch.OnClientDisconnected(e.Peer.PlayerID);
            }
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID || !Context.IsMainPlayer) return;
            if (e.Type != RequestSyncId && e.Type != StopSyncId) return;

            string name = Game1.GetPlayer(e.FromPlayerID)?.Name ?? "Unknown";

            if (e.Type == RequestSyncId)
            {
                Monitor.Log($"[Host] Received RequestSync from {name} ({e.FromPlayerID})", LogLevel.Debug);
                NPCMapLocationsPatch.OnClientRequestSync(e.FromPlayerID);
            }
            else
            {
                Monitor.Log($"[Host] Received StopSync from {name} ({e.FromPlayerID})", LogLevel.Debug);
                NPCMapLocationsPatch.OnClientStopSync(e.FromPlayerID);
            }
        }

        private static int MapTabIndex => Constants.TargetPlatform == GamePlatform.Android ? 4 : GameMenu.mapTab;

        private static bool IsMapMenuOpen()
        {
            var menu = Game1.activeClickableMenu;

            if (menu is GameMenu gm)
            {
                if (gm.currentTab != MapTabIndex)
                    return false;

                var pages = (List<IClickableMenu>?)PagesField?.GetValue(gm);
                IClickableMenu? page = pages is { Count: > 0 } && gm.currentTab < pages.Count
                    ? pages[gm.currentTab]
                    : null;
                return page is MapPage || page?.GetType().Name == "ModMapPage";
            }

            return menu is MapPage || menu?.GetType().Name == "ModMapPage";
        }

        private void SetMapOpen(bool open)
        {
            if (IsMapOpen == open) return;
            IsMapOpen = open;

            // Farmhands tell the host when they start/stop needing NPC sync
            if (!Context.IsMultiplayer || Context.IsMainPlayer) return;

            if (open)
            {
                Monitor.Log("[Client] Map opened - sending RequestSync", LogLevel.Debug);
                SendSyncMessage(RequestSyncId);
            }
            else
            {
                Monitor.Log("[Client] Map closed - sending StopSync", LogLevel.Debug);
                SendSyncMessage(StopSyncId);
            }
        }

        private void SendSyncMessage(string msgType)
        {
            long hostId = GetHostId();
            if (hostId == -1)
            {
                Monitor.Log("[Client] Failed to send sync request: host ID not found", LogLevel.Warn);
                return;
            }

            Helper.Multiplayer.SendMessage(
                message: new object(),
                messageType: msgType,
                modIDs: new[] { ModManifest.UniqueID },
                playerIDs: new[] { hostId }
            );
        }

        private long GetHostId()
        {
            if (cachedHostId.HasValue)
                return cachedHostId.Value;

            foreach (var peer in Helper.Multiplayer.GetConnectedPlayers())
            {
                if (peer.IsHost)
                {
                    cachedHostId = peer.PlayerID;
                    return peer.PlayerID;
                }
            }

            return -1;
        }
    }
}
