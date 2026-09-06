using System;
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
        private static bool IsMapOpen;
        private static long? CachedHostId;
        private static Assembly? NpcAssembly;

        // Message IDs - use actual Mod UniqueID prefix
        private const string MsgPrefix = "NPCMapLocationsPerformancePatch";
        private const string RequestSyncId = MsgPrefix + ".RequestSync";
        private const string StopSyncId = MsgPrefix + ".StopSync";

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            var harmony = new Harmony(ModManifest.UniqueID);

            try
            {
                NpcAssembly = FindNpcAssembly();
                if (NpcAssembly != null)
                {
                    PatchUpdateTicked(harmony);
                    PatchMessageReceived(harmony);
                }
                Monitor.Log("NPC Map Locations Performance Patch loaded!", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Init error: {ex}", LogLevel.Error);
            }

            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
            helper.Events.Multiplayer.PeerDisconnected += OnPeerDisconnected;
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        }

        private Assembly? FindNpcAssembly()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                if (asm.GetName().Name == "NPCMapLocations")
                    return asm;
            Monitor.Log("NPCMapLocations assembly not found!", LogLevel.Error);
            return null;
        }

        private void PatchUpdateTicked(Harmony harmony)
        {
            // NPCMapLocations.ModEntry.OnUpdateTicked is the actual method
            var type = NpcAssembly!.GetType("NPCMapLocations.ModEntry");
            if (type == null)
            {
                Monitor.Log("NPCMapLocations.ModEntry type not found", LogLevel.Error);
                return;
            }

            var method = type.GetMethod("OnUpdateTicked", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                // Fallback: try other common names
                foreach (var name in new[] { "GameLoop_UpdateTicked", "UpdateTicked" })
                {
                    method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null) break;
                }
            }

            if (method != null)
            {
                var prefix = typeof(NPCMapLocationsPatch).GetMethod(nameof(NPCMapLocationsPatch.UpdatePrefix));
                harmony.Patch(method, new HarmonyMethod(prefix));
                Monitor.Log($"Patched: {type.Name}.{method.Name}", LogLevel.Debug);
            }
            else
            {
                Monitor.Log("Could not find UpdateTicked method to patch", LogLevel.Warn);
            }
        }

        private void PatchMessageReceived(Harmony harmony)
        {
            var type = NpcAssembly!.GetType("NPCMapLocations.ModEntry");
            if (type == null) return;

            var method = type.GetMethod("OnModMessageReceived", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method != null)
            {
                var postfix = typeof(NPCMapLocationsPatch).GetMethod(nameof(NPCMapLocationsPatch.OnModMessageReceivedPostfix));
                if (postfix != null)
                {
                    harmony.Patch(method, null, new HarmonyMethod(postfix));
                    Monitor.Log("Patched OnModMessageReceived for on-demand sync", LogLevel.Debug);
                }
            }
        }

        private void OnPeerConnected(object? _, PeerConnectedEventArgs e)
        {
            if (e.Peer.IsHost) CachedHostId = e.Peer.PlayerID;
        }

        private void OnPeerDisconnected(object? _, PeerDisconnectedEventArgs e)
        {
            if (e.Peer.IsHost) CachedHostId = null;
        }

        private void OnModMessageReceived(object? _, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID || !Context.IsMainPlayer) return;

            if (e.Type == RequestSyncId)
                NPCMapLocationsPatch.OnClientRequestSync(e.FromPlayerID);
            else if (e.Type == StopSyncId)
                NPCMapLocationsPatch.OnClientStopSync(e.FromPlayerID);
        }

        private void OnMenuChanged(object? _, MenuChangedEventArgs e)
        {
            var menu = e.NewMenu;

            // GameMenu tab switch detection
            if (menu?.GetType().Name == "GameMenu")
            {
                try
                {
                    var tabField = menu.GetType().GetField("currentTab", BindingFlags.Instance | BindingFlags.Public);
                    if (tabField?.GetValue(menu) is int tab)
                    {
                        SetMapOpen(tab == 3); // MapTabIndex = 3
                    }
                }
                catch { }
                return;
            }

            // Direct map page open/close
            string? menuTypeName = menu?.GetType().Name;
            bool isMapPage = menuTypeName is "MapPage" or "ModMapPage";

            if (isMapPage)
                SetMapOpen(true);
            else if (menu == null && IsMapOpen)
                SetMapOpen(false);
        }

        private void SetMapOpen(bool open)
        {
            if (IsMapOpen == open) return;
            bool wasOpen = IsMapOpen;
            IsMapOpen = open;

            if (Context.IsMultiplayer && !Context.IsMainPlayer)
            {
                if (open && !wasOpen)
                    SendSyncRequest(RequestSyncId);
                else if (!open && wasOpen)
                    SendSyncRequest(StopSyncId);
            }
        }

        private void SendSyncRequest(string msgType)
        {
            if (CachedHostId is not long hostId) return;

            Helper.Multiplayer.SendMessage(
                message: (object?)null,
                messageType: msgType,
                modIDs: new[] { ModManifest.UniqueID },
                playerIDs: new[] { hostId }
            );
        }

        public static bool GetIsMapOpen() => IsMapOpen;
    }
}