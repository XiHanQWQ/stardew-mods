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
        private static readonly Dictionary<long, int> SyncRequests = new();
        private static int totalRequests = 0; // Cached counter

        public static bool UpdatePrefix()
        {
            if (Context.IsMainPlayer)
                return totalRequests > 0 || ModEntry.GetIsMapOpen();

            return ModEntry.GetIsMapOpen();
        }

        public static void OnClientRequestSync(long clientId)
        {
            if (!Context.IsMainPlayer) return;

            int newCount = SyncRequests.TryGetValue(clientId, out int c) ? c + 1 : 1;
            SyncRequests[clientId] = newCount;
            totalRequests++;

            ModEntry.ModMonitor?.Log($"[Patch] Host: Client {clientId} requested NPC sync (total: {totalRequests})", LogLevel.Debug);
        }

        public static void OnClientStopSync(long clientId)
        {
            if (!Context.IsMainPlayer) return;

            if (SyncRequests.TryGetValue(clientId, out int count))
            {
                if (count <= 1)
                    SyncRequests.Remove(clientId);
                else
                    SyncRequests[clientId] = count - 1;

                totalRequests = Math.Max(0, totalRequests - 1);
                ModEntry.ModMonitor?.Log($"[Patch] Host: Client {clientId} stopped NPC sync (total: {totalRequests})", LogLevel.Debug);
            }
        }

        public static int GetRequestCount() => totalRequests;
    }
}