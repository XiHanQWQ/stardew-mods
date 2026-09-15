using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace NPCMapLocationsPerformancePatch
{
    [HarmonyPatch]
    public static class NPCMapLocationsPatch
    {
        private static readonly object SyncLock = new object();
        private static readonly Dictionary<long, int> SyncRequests = new Dictionary<long, int>();
        private static int totalRequests = 0; // Cached counter

        /// <summary>Harmony prefix for NPCMapLocations.ModEntry.UpdateTicked.</summary>
        public static bool UpdatePrefix()
        {
            if (Context.IsMainPlayer)
            {
                lock (SyncLock)
                {
                    return totalRequests > 0 || ModEntry.IsMapOpen;
                }
            }
            return ModEntry.IsMapOpen;
        }

        /// <summary>Called by host when a client requests NPC sync.</summary>
        public static void OnClientRequestSync(long clientId)
        {
            if (!Context.IsMainPlayer) return;

            lock (SyncLock)
            {
                SyncRequests.TryGetValue(clientId, out int count);
                SyncRequests[clientId] = count + 1;
                totalRequests++;
                ModEntry.ModMonitor?.Log($"[Patch] Host: Client {clientId} requested NPC sync (total: {totalRequests})", LogLevel.Debug);
            }
        }

        /// <summary>Called by host when a client stops NPC sync.</summary>
        public static void OnClientStopSync(long clientId)
        {
            if (!Context.IsMainPlayer) return;

            lock (SyncLock)
            {
                if (SyncRequests.TryGetValue(clientId, out int count))
                {
                    if (count <= 1)
                        SyncRequests.Remove(clientId);
                    else
                        SyncRequests[clientId] = count - 1;

                    totalRequests = Math.Max(0, totalRequests - 1);
                    ModEntry.ModMonitor?.Log($"[Patch] Host: Client {clientId} stopped NPC sync (total: {totalRequests})", LogLevel.Debug);
                }
                else
                {
                    ModEntry.ModMonitor?.Log($"[Patch] Host: StopSync received from client {clientId} but no active request found.", LogLevel.Warn);
                }
            }
        }

        /// <summary>Called by host when a client disconnects to clean up their requests.</summary>
        public static void OnClientDisconnected(long clientId)
        {
            if (!Context.IsMainPlayer) return;

            lock (SyncLock)
            {
                if (SyncRequests.TryGetValue(clientId, out int count))
                {
                    SyncRequests.Remove(clientId);
                    totalRequests = Math.Max(0, totalRequests - count);
                    ModEntry.ModMonitor?.Log($"[Patch] Host: Client {clientId} disconnected, removed {count} sync request(s) (total: {totalRequests})", LogLevel.Debug);
                }
            }
        }

        /// <summary>Gets total active sync requests (host only).</summary>
        public static int GetRequestCount()
        {
            lock (SyncLock)
            {
                return totalRequests;
            }
        }
    }
}