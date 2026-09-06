using System;
using System.Reflection;
using System.Collections.Generic;
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

        // Message IDs for on-demand sync
        private const string RequestSyncMessageId = "NPCMapLocationsPatch.RequestSync";
        private const string StopSyncMessageId = "NPCMapLocationsPatch.StopSync";

        public override void Entry(IModHelper helper)
        {
            ModEntry.ModMonitor = base.Monitor;
            Harmony harmony = new Harmony(base.ModManifest.UniqueID);
            
            try
            {
                this.PatchNPCMapLocations(harmony);
                this.PatchMessageHandling(harmony);
                base.Monitor.Log("Performance Patch for NPC Map Locations loaded successfully!", LogLevel.Info);
                base.Monitor.Log("NPC positions will update on-demand when any client opens the map.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                base.Monitor.Log("Error during initialization: " + ex.Message, LogLevel.Error);
            }

            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
            helper.Events.Multiplayer.PeerDisconnected += this.OnPeerDisconnected;
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        }

        private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
        {
            // Cache host ID when connected
            if (e.Peer.IsHost)
                CachedHostId = e.Peer.PlayerID;
        }

        private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
        {
            if (e.Peer.IsHost)
                CachedHostId = null;
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != this.ModManifest.UniqueID)
                return;

            // Handle messages from clients (when we are host)
            if (Context.IsMainPlayer)
            {
                switch (e.Type)
                {
                    case RequestSyncMessageId:
                        NPCMapLocationsPatch.OnClientRequestSync(e.FromPlayerID);
                        break;
                    case StopSyncMessageId:
                        NPCMapLocationsPatch.OnClientStopSync(e.FromPlayerID);
                        break;
                }
            }
        }

        private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            IClickableMenu? menu = Game1.activeClickableMenu;
            if (menu != null)
            {
                string menuType = menu.GetType().Name;
                bool mapOpened = (menuType == "MapPage" || menuType.Contains("Map")) && !ModEntry.IsMapOpen;
                if (mapOpened)
                {
                    this.SetMapOpen(true);
                }
                bool isGameMenu = menuType == "GameMenu";
                if (isGameMenu)
                {
                    this.CheckGameMenuTab();
                }
            }
            else
            {
                if (ModEntry.IsMapOpen)
                {
                    this.SetMapOpen(false);
                }
            }
        }

        private void SetMapOpen(bool open)
        {
            bool wasOpen = ModEntry.IsMapOpen;
            ModEntry.IsMapOpen = open;

            // Only send sync requests in multiplayer as a farmhand (client)
            if (Context.IsMultiplayer && !Context.IsMainPlayer)
            {
                if (open && !wasOpen)
                {
                    this.SendRequestSync();
                }
                else if (!open && wasOpen)
                {
                    this.SendStopSync();
                }
            }
        }

        private void SendRequestSync()
        {
            long hostId = this.GetHostId();
            if (hostId == -1)
                return;

            this.Helper.Multiplayer.SendMessage(
                message: new object(), // empty payload
                messageType: RequestSyncMessageId,
                modIDs: new[] { this.ModManifest.UniqueID },
                playerIDs: new[] { hostId }
            );
            ModMonitor?.Log($"[Patch] Client requested NPC sync from host", LogLevel.Debug);
        }

        private void SendStopSync()
        {
            long hostId = this.GetHostId();
            if (hostId == -1)
                return;

            this.Helper.Multiplayer.SendMessage(
                message: new object(),
                messageType: StopSyncMessageId,
                modIDs: new[] { this.ModManifest.UniqueID },
                playerIDs: new[] { hostId }
            );
            ModMonitor?.Log($"[Patch] Client stopped NPC sync request", LogLevel.Debug);
        }

        private long GetHostId()
        {
            if (CachedHostId.HasValue)
                return CachedHostId.Value;

            foreach (IMultiplayerPeer peer in this.Helper.Multiplayer.GetConnectedPlayers())
            {
                if (peer.IsHost)
                {
                    CachedHostId = peer.PlayerID;
                    return peer.PlayerID;
                }
            }
            return -1;
        }

        private void PatchNPCMapLocations(Harmony harmony)
        {
            IModInfo? npcMod = base.Helper.ModRegistry.Get("Bouhm.NPCMapLocations");
            if (npcMod == null)
            {
                base.Monitor.Log("NPC Map Locations mod not found! This patch requires it to be installed.", LogLevel.Error);
                return;
            }

            Assembly? assembly = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "NPCMapLocations")
                {
                    assembly = asm;
                    break;
                }
            }

            if (assembly == null)
            {
                base.Monitor.Log("Could not find NPC Map Locations assembly!", LogLevel.Error);
                return;
            }

            int patched = 0;
            string[] targetMethods = new string[] { "GameLoop_UpdateTicked", "OnUpdateTicked", "UpdateTicked" };
            foreach (Type? type in assembly.GetTypes())
            {
                if (type == null) continue;
                foreach (string targetMethodName in targetMethods)
                {
                    MethodInfo? method = type.GetMethod(targetMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        try
                        {
                            ParameterInfo[] parameters = method.GetParameters();
                            if (parameters.Length == 2)
                            {
                                MethodInfo? prefix = typeof(NPCMapLocationsPatch).GetMethod("UpdatePrefix");
                                if (prefix != null)
                                {
                                    harmony.Patch(method, new HarmonyMethod(prefix), null, null, null);
                                    base.Monitor.Log("Patched: " + type.Name + "." + method.Name, LogLevel.Debug);
                                    patched++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            base.Monitor.Log("Failed to patch " + targetMethodName + ": " + ex.Message, LogLevel.Warn);
                        }
                    }
                }
            }

            if (patched > 0)
            {
                base.Monitor.Log($"Successfully patched {patched} update method(s).", LogLevel.Info);
            }
            else
            {
                base.Monitor.Log("Warning: Could not find update event handlers to patch.", LogLevel.Warn);
            }
        }

        private void PatchMessageHandling(Harmony harmony)
        {
            IModInfo? npcMod = base.Helper.ModRegistry.Get("Bouhm.NPCMapLocations");
            if (npcMod == null)
                return;

            Assembly? assembly = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "NPCMapLocations")
                {
                    assembly = asm;
                    break;
                }
            }

            if (assembly == null)
                return;

            // Find ModEntry type and OnModMessageReceived method
            foreach (Type? type in assembly.GetTypes())
            {
                if (type == null) continue;
                if (type.Name == "ModEntry")
                {
                    MethodInfo? method = type.GetMethod("OnModMessageReceived", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (method != null)
                    {
                        try
                        {
                            MethodInfo? postfix = typeof(NPCMapLocationsPatch).GetMethod("OnModMessageReceivedPostfix");
                            if (postfix != null)
                            {
                                harmony.Patch(method, null, new HarmonyMethod(postfix));
                                base.Monitor.Log("Patched OnModMessageReceived for on-demand sync", LogLevel.Debug);
                            }
                        }
                        catch (Exception ex)
                        {
                            base.Monitor.Log("Failed to patch OnModMessageReceived: " + ex.Message, LogLevel.Warn);
                        }
                    }
                    break;
                }
            }
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            IClickableMenu? newMenu = e.NewMenu;
            if (newMenu != null && newMenu.GetType().Name == "GameMenu")
            {
                this.CheckGameMenuTab();
            }
            if (newMenu != null && (newMenu.GetType().Name == "MapPage" || newMenu.GetType().Name.Contains("Map")))
            {
                this.SetMapOpen(true);
            }
            if (newMenu == null && ModEntry.IsMapOpen)
            {
                this.SetMapOpen(false);
            }
        }

        private void CheckGameMenuTab()
        {
            try
            {
                IClickableMenu? menu = Game1.activeClickableMenu;
                if (menu != null)
                {
                    Type gameMenuType = menu.GetType();
                    FieldInfo? currentTabField = gameMenuType.GetField("currentTab", BindingFlags.Instance | BindingFlags.Public);
                    if (currentTabField != null)
                    {
                        object? value = currentTabField.GetValue(menu);
                        if (value is int currentTab)
                        {
                            this.SetMapOpen(currentTab == 3);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public static bool GetIsMapOpen()
        {
            return ModEntry.IsMapOpen;
        }
    }
}