using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace NPCMapLocationsPerformancePatch
{
    /// <summary>Gates NPC Map Locations' per-tick NPC sync so it only runs while someone can actually see the map.</summary>
    public static class NPCMapLocationsPatch
    {
        private static readonly object SyncLock = new();
        private static readonly HashSet<long> SyncClients = new();

        // Read every tick by the Harmony prefix without taking the lock; volatile so
        // state changes made under SyncLock are immediately visible to that hot path.
        private static volatile bool HasClientRequests;

        /// <summary>Harmony prefix for NPCMapLocations.ModEntry's UpdateTicked handler. Returns true to let the original sync run.</summary>
        public static bool UpdatePrefix()
        {
            if (Context.IsMainPlayer)
                return HasClientRequests || ModEntry.IsMapOpen;
            return ModEntry.IsMapOpen;
        }

        /// <summary>Called on the host when a client requests NPC sync (client opened the map).</summary>
        public static void OnClientRequestSync(long clientId)
        {
            if (!Context.IsMainPlayer) return;

            lock (SyncLock)
            {
                SyncClients.Add(clientId);
                HasClientRequests = true;
                ModEntry.ModMonitor?.Log($"[Patch] Host: client {clientId} requested NPC sync ({SyncClients.Count} active)", LogLevel.Debug);
            }
        }

        /// <summary>Called on the host when a client stops requesting NPC sync (client closed the map).</summary>
        public static void OnClientStopSync(long clientId)
        {
            if (!Context.IsMainPlayer) return;

            lock (SyncLock)
            {
                if (SyncClients.Remove(clientId))
                {
                    HasClientRequests = SyncClients.Count > 0;
                    ModEntry.ModMonitor?.Log($"[Patch] Host: client {clientId} stopped NPC sync ({SyncClients.Count} active)", LogLevel.Debug);
                }
                else
                {
                    ModEntry.ModMonitor?.Log($"[Patch] Host: StopSync received from client {clientId} but no active request found.", LogLevel.Warn);
                }
            }
        }

        /// <summary>Called on the host when a client leaves, so their request doesn't keep NPC sync running.</summary>
        public static void OnClientDisconnected(long clientId)
        {
            if (!Context.IsMainPlayer) return;

            lock (SyncLock)
            {
                if (SyncClients.Remove(clientId))
                {
                    HasClientRequests = SyncClients.Count > 0;
                    ModEntry.ModMonitor?.Log($"[Patch] Host: client {clientId} disconnected, removed sync request ({SyncClients.Count} active)", LogLevel.Debug);
                }
            }
        }

        /// <summary>Clears all sync state (host returned to title).</summary>
        public static void Reset()
        {
            lock (SyncLock)
            {
                SyncClients.Clear();
                HasClientRequests = false;
            }
        }
    }
}
