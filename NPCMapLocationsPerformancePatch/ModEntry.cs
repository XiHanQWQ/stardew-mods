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
        public static bool IsMapOpen { get; private set; } // Made static with private setter

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

            InitializeHostId();
        }

        private void InitializeHostId()
        {
            try
            {
                foreach (var peer in Helper.Multiplayer.GetConnectedPlayers())
                {
                    if (peer.IsHost)
                    {
                        CachedHostId = peer.PlayerID;
                        Monitor.Log($"Host ID initialized: {peer.PlayerID}", LogLevel.Debug);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to initialize host ID: {ex.Message}", LogLevel.Warn);
            }
        }

        private Assembly? FindNpcAssembly()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "NPCMapLocations")
                    return asm;
            }
            Monitor.Log("NPCMapLocations assembly not found!", LogLevel.Error);
            return null;
        }

        private void PatchUpdateTicked(Harmony harmony)
        {
            var type = NpcAssembly!.GetType("NPCMapLocations.ModEntry");
            if (type == null)
            {
                Monitor.Log("NPCMapLocations.ModEntry type not found", LogLevel.Error);
                return;
            }

            var method = type.GetMethod("OnUpdateTicked", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
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

        private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
        {
            if (e.Peer.IsHost)
            {
                CachedHostId = e.Peer.PlayerID;
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
                CachedHostId = null;
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

            string name = "Unknown";
            var farmer = Game1.GetPlayer(e.FromPlayerID);
            if (farmer != null)
                name = farmer.Name;

            if (e.Type == RequestSyncId)
            {
                Monitor.Log($"[Host] Received RequestSync from {name} ({e.FromPlayerID})", LogLevel.Debug);
                NPCMapLocationsPatch.OnClientRequestSync(e.FromPlayerID);
            }
            else if (e.Type == StopSyncId)
            {
                Monitor.Log($"[Host] Received StopSync from {name} ({e.FromPlayerID})", LogLevel.Debug);
                NPCMapLocationsPatch.OnClientStopSync(e.FromPlayerID);
            }
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            var menu = e.NewMenu;

            if (menu?.GetType().Name == "GameMenu")
            {
                try
                {
                    var tabField = menu.GetType().GetField("currentTab", BindingFlags.Instance | BindingFlags.Public);
                    if (tabField?.GetValue(menu) is int tab)
                    {
                        SetMapOpen(tab == 3); // 3 = Map tab in vanilla GameMenu
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Failed to inspect GameMenu: {ex.Message}", LogLevel.Debug);
                }
                return;
            }

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
                {
                    Monitor.Log("[Client] Map opened - sending RequestSync", LogLevel.Debug);
                    SendSyncRequest(RequestSyncId);
                }
                else if (!open && wasOpen)
                {
                    Monitor.Log("[Client] Map closed - sending StopSync", LogLevel.Debug);
                    SendSyncRequest(StopSyncId);
                }
            }
        }

        private void SendSyncRequest(string msgType)
        {
            long hostId = GetHostId();
            if (hostId == -1)
            {
                Monitor.Log("[Client] Failed to send sync request: host ID not found", LogLevel.Warn);
                return;
            }

            var host = Game1.GetPlayer(hostId);
            string hostName = host?.Name ?? "Host";
            Monitor.Log($"[Client] Sending {msgType} to {hostName} ({hostId})", LogLevel.Debug);

            Helper.Multiplayer.SendMessage(
                message: new object(),
                messageType: msgType,
                modIDs: new[] { ModManifest.UniqueID },
                playerIDs: new[] { hostId }
            );
        }

        private long GetHostId()
        {
            if (CachedHostId.HasValue)
                return CachedHostId.Value;

            try
            {
                foreach (var peer in Helper.Multiplayer.GetConnectedPlayers())
                {
                    if (peer.IsHost)
                    {
                        CachedHostId = peer.PlayerID;
                        return peer.PlayerID;
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error getting host ID: {ex.Message}", LogLevel.Warn);
            }

            return -1;
        }
    }
}