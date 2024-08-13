using System;
using System.Threading;
using BepInEx.Logging;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using Systems.Effects;
using UnityEngine;
using static Donuts.DonutComponent;

namespace Donuts
{
    internal class DonutDespawnLogic : MonoBehaviour
    {
        internal static ManualLogSource Logger
        {
            get; private set;
        }

        public DonutDespawnLogic()
        {
            Logger ??= BepInEx.Logging.Logger.CreateLogSource(nameof(DonutDespawnLogic));
        }

        internal static async UniTask DespawnFurthestBot(string bottype, CancellationToken cancellationToken)
        {
            //Debug.Log($"DespawnFurthestBot: Started despawn logic for {bottype} bots.");

            float despawnCooldown = bottype == "pmc" ? PMCdespawnCooldown : SCAVdespawnCooldown;
            float despawnCooldownDuration = bottype == "pmc" ? PMCdespawnCooldownDuration : SCAVdespawnCooldownDuration;

            float currentTime = Time.time;
            float timeSinceLastDespawn = currentTime - despawnCooldown;

            //Debug.Log($"DespawnFurthestBot: Current time: {currentTime}, Time since last despawn: {timeSinceLastDespawn}");

            if (timeSinceLastDespawn < despawnCooldownDuration)
            {
                //Debug.Log($"DespawnFurthestBot: Cooldown not yet expired. Cooldown duration: {despawnCooldownDuration}");
                return;
            }

            if (!await ShouldConsiderDespawning(bottype, cancellationToken))
            {
                //Debug.Log($"DespawnFurthestBot: Despawning not needed for {bottype} bots.");
                return;
            }

            Debug.Log($"DespawnFurthestBot: Despawning needed for {bottype} bots.");

            Player furthestBot = await UpdateDistancesAndFindFurthestBot(bottype);

            if (furthestBot != null)
            {
                Debug.Log($"DespawnFurthestBot: Furthest bot found: {furthestBot.Profile.Info.Nickname}. Proceeding to despawn.");
                DespawnBot(furthestBot, bottype);
            }
            else
            {
                Debug.LogWarning($"DespawnFurthestBot: No {bottype} bot found to despawn.");
            }
        }

        private static void DespawnBot(Player furthestBot, string bottype)
        {
            //Debug.Log($"DespawnBot: Despawning bot: {furthestBot?.Profile.Info.Nickname}");

            if (furthestBot == null)
            {
                Debug.LogError("DespawnBot: Attempted to despawn a null bot.");
                return;
            }

            BotOwner botOwner = furthestBot.AIData.BotOwner;
            if (botOwner == null)
            {
                Debug.LogError("DespawnBot: BotOwner is null for the furthest bot.");
                return;
            }

            Debug.Log($"DespawnBot: Despawning bot: {furthestBot.Profile.Info.Nickname} ({furthestBot.name})");

            gameWorld.RegisteredPlayers.Remove(botOwner);
            gameWorld.AllAlivePlayersList.Remove(botOwner.GetPlayer);

            var botgame = Singleton<IBotGame>.Instance;
            Singleton<Effects>.Instance.EffectsCommutator.StopBleedingForPlayer(botOwner.GetPlayer);
            botOwner.Deactivate();
            botOwner.Dispose();
            botgame.BotsController.BotDied(botOwner);
            botgame.BotsController.DestroyInfo(botOwner.GetPlayer);
            DestroyImmediate(botOwner.gameObject);
            Destroy(botOwner);

            Debug.Log($"DespawnBot: Bot {furthestBot.Profile.Info.Nickname} despawned successfully.");

            // Update the cooldown
            if (bottype == "pmc")
            {
                PMCdespawnCooldown = Time.time;
                Debug.Log("DespawnBot: Updated PMC despawn cooldown.");
            }
            else if (bottype == "scav")
            {
                SCAVdespawnCooldown = Time.time;
                Debug.Log("DespawnBot: Updated SCAV despawn cooldown.");
            }
        }

        private static async UniTask<bool> ShouldConsiderDespawning(string botType, CancellationToken cancellationToken)
        {
            //Debug.Log($"ShouldConsiderDespawning: Checking if despawning is needed for {botType} bots.");

            int botLimit = botType == "pmc" ? DonutComponent.PMCBotLimit : DonutComponent.SCAVBotLimit;
            int activeBotCount = await BotCountManager.GetAlivePlayers(botType, cancellationToken);

            //Debug.Log($"ShouldConsiderDespawning: Active {botType} bot count: {activeBotCount}, Limit: {botLimit}");

            return activeBotCount > botLimit; // Only consider despawning if the number of active bots of the type exceeds the limit
        }

        private static UniTask<Player> UpdateDistancesAndFindFurthestBot(string bottype)
        {
            return UniTask.Create(async () =>
            {
                //Debug.Log($"UpdateDistancesAndFindFurthestBot: Calculating distances for {bottype} bots.");

                float maxDistance = float.MinValue;
                Player furthestBot = null;

                foreach (var bot in gameWorld.AllAlivePlayersList)
                {
                    if (!bot.IsYourPlayer && bot.AIData.BotOwner != null && IsBotType(bot, bottype))
                    {
                        // Get distance of bot to player using squared distance
                        float distance = (mainplayer.Transform.position - bot.Transform.position).sqrMagnitude;

                        // Check if this is the furthest distance
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                            furthestBot = bot;
                        }
                    }
                }

                if (furthestBot == null)
                {
                    Debug.LogWarning("UpdateDistancesAndFindFurthestBot: Furthest bot is null. No bots found in the list.");
                }
                else
                {
                    //Debug.Log($"UpdateDistancesAndFindFurthestBot: Furthest bot found: {furthestBot.Profile.Info.Nickname} at distance {Mathf.Sqrt(maxDistance)}");
                }

                return furthestBot;
            });
        }

        private static bool IsBotType(Player bot, string bottype)
        {
            //Debug.Log($"IsBotType: Checking if bot {bot.Profile.Info.Nickname} is of type {bottype}.");

            switch (bottype)
            {
                case "scav":
                    return BotCountManager.IsSCAV(bot.Profile.Info.Settings.Role);
                case "pmc":
                    return BotCountManager.IsPMC(bot.Profile.Info.Settings.Role);
                default:
                    throw new ArgumentException("Invalid bot type", nameof(bottype));
            }
        }
    }
}
