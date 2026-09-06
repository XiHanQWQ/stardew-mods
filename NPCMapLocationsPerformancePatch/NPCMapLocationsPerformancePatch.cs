using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace NPCMapLocationsPerformancePatch
{
    [HarmonyPatch]
    public static class NPCMapLocationsPatch
    {
        // Track which clients have requested sync (playerID -> request count)
        private static readonly Dictionary<long, int> SyncRequests = new();
        private static int TotalRequests => SyncRequests.Values.Sum();

        /// <summary>Prefix for UpdateTicked - only runs if host has pending sync requests or local player has map open.</summary>
        public static bool UpdatePrefix()
        {
            // Host: run update if any client requested sync OR if host has map open
            if (Context.IsMainPlayer)
            {
                return TotalRequests > 0 || ModEntry.GetIsMapOpen();
            }

            // Farmhand (client): only run update if local map is open
            return ModEntry.GetIsMapOpen();
        }

        /// <summary>Called by host when a client requests NPC sync.</summary>
        public static void OnClientRequestSync(long clientId)
        {
            if (!Context.IsMainPlayer)
                return;

            if (SyncRequests.TryGetValue(clientId, out int count))
                SyncRequests[clientId] = count + 1;
            else
                SyncRequests[clientId] = 1;

            ModEntry.ModMonitor?.Log($"[Patch] Host: Client {clientId} requested NPC sync (total requests: {TotalRequests})", LogLevel.Debug);
        }

        /// <summary>Called by host when a client stops requesting NPC sync.</summary>
        public static void OnClientStopSync(long clientId)
        {
            if (!Context.IsMainPlayer)
                return;

            if (SyncRequests.TryGetValue(clientId, out int count))
            {
                if (count <= 1)
                    SyncRequests.Remove(clientId);
                else
                    SyncRequests[clientId] = count - 1;

                ModEntry.ModMonitor?.Log($"[Patch] Host: Client {clientId} stopped NPC sync (total requests: {TotalRequests})", LogLevel.Debug);
            }
        }

        /// <summary>Postfix injected into host's OnModMessageReceived to handle RequestSync/StopSync messages.</summary>
        public static void OnModMessageReceivedPostfix(object __instance, StardewModdingAPI.Events.ModMessageReceivedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;

            const string RequestSyncId = "NPCMapLocationsPatch.RequestSync";
            const string StopSyncId = "NPCMapLocationsPatch.StopSync";

            if (e.FromModID != "NPCMapLocationsPatch") // Our patch mod's ID
                return;

            switch (e.Type)
            {
                case RequestSyncId:
                    OnClientRequestSync(e.FromPlayerID);
                    break;
                case StopSyncId:
                    OnClientStopSync(e.FromPlayerID);
                    break;
            }
        }

        /// <summary>Get current request count for debugging.</summary>
        public static int GetRequestCount() => TotalRequests;
    }
}