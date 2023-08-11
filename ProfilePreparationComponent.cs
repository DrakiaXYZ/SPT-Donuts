﻿using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Aki.PrePatch;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Bots;
using HarmonyLib;
using UnityEngine;

#pragma warning disable IDE0007, CS4014

namespace Donuts
{
    internal class DonutsBotPrep : MonoBehaviour
    {
        private static GameWorld gameWorld;
        private static IBotCreator botCreator;
        private static BotSpawnerClass botSpawnerClass;
        private static CancellationTokenSource cancellationToken;

        private static WildSpawnType sptUsec;
        private static WildSpawnType sptBear;

        internal static List<GClass628> bearsEasy;
        internal static List<GClass628> usecEasy;
        internal static List<GClass628> assaultEasy;

        internal static List<GClass628> bearsNormal;
        internal static List<GClass628> usecNormal;
        internal static List<GClass628> assaultNormal;

        internal static List<GClass628> bearsHard;
        internal static List<GClass628> usecHard;
        internal static List<GClass628> assaultHard;

        internal static List<GClass628> bearsImpossible;
        internal static List<GClass628> usecImpossible;
        internal static List<GClass628> assaultImpossible;

        private float timeSinceLastReplenish;
        private int maxBotCount;
        private float replenishInterval;
        internal static ManualLogSource Logger
        {
            get; private set;
        }

        public DonutsBotPrep()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(DonutsBotPrep));
            }

        }
        public static void Enable()
        {
            gameWorld = Singleton<GameWorld>.Instance;
            gameWorld.GetOrAddComponent<DonutsBotPrep>();

            Logger.LogDebug("DonutBotPrep Enabled");
        }

        public async void Awake()
        {
            //init the main vars
            botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
            botCreator = AccessTools.Field(typeof(BotSpawnerClass), "ginterface17_0").GetValue(botSpawnerClass) as IBotCreator;
            cancellationToken = AccessTools.Field(typeof(BotSpawnerClass), "cancellationTokenSource_0").GetValue(botSpawnerClass) as CancellationTokenSource;
            sptUsec = (WildSpawnType)AkiBotsPrePatcher.sptUsecValue;
            sptBear = (WildSpawnType)AkiBotsPrePatcher.sptBearValue;
            timeSinceLastReplenish = 0f;
            maxBotCount = 5;
            replenishInterval = 60.0f;
        }

        

        private async void Start()
        {
            // Initialize the bot pool at the beginning of the round
            await InitializeBotPool();
        }

        private async Task InitializeBotPool()
        {
            //init bots for diff difficulties
            //read the DonutsPlugin
            bearsEasy = new List<GClass628>();
            usecEasy = new List<GClass628>();
            assaultEasy = new List<GClass628>();
            bearsNormal = new List<GClass628>();
            usecNormal = new List<GClass628>();
            assaultNormal = new List<GClass628>();
            bearsHard = new List<GClass628>();
            usecHard = new List<GClass628>();
            assaultHard = new List<GClass628>();
            bearsImpossible = new List<GClass628>();
            usecImpossible = new List<GClass628>();
            assaultImpossible = new List<GClass628>();

            Logger.LogWarning("Profile Generation is Creating for Donuts Difficulty: " + DonutsPlugin.botDifficulties.Value.ToLower());
            if (DonutsPlugin.botDifficulties.Value.ToLower() == "asonline")
            {
                //create as online mix
                CreateBots(bearsNormal, EPlayerSide.Bear, sptBear, BotDifficulty.normal, 3);
                CreateBots(usecNormal, EPlayerSide.Usec, sptUsec, BotDifficulty.normal, 3);
                CreateBots(assaultNormal, EPlayerSide.Savage, WildSpawnType.assault, BotDifficulty.normal, 3);
                CreateBots(bearsHard, EPlayerSide.Bear, sptBear, BotDifficulty.hard, 2);
                CreateBots(usecHard, EPlayerSide.Usec, sptUsec, BotDifficulty.hard, 2);
                CreateBots(assaultHard, EPlayerSide.Savage, WildSpawnType.assault, BotDifficulty.hard, 2);
            }
            else if (DonutsPlugin.botDifficulties.Value.ToLower() == "easy")
            {
                //create Easy bots to spawn with
                CreateBots(bearsEasy, EPlayerSide.Bear, sptBear, BotDifficulty.easy, 5);
                CreateBots(usecEasy, EPlayerSide.Usec, sptUsec, BotDifficulty.easy, 5);
                CreateBots(assaultEasy, EPlayerSide.Savage, WildSpawnType.assault, BotDifficulty.easy, 5);
            }
            else if (DonutsPlugin.botDifficulties.Value.ToLower() == "normal")
            {
                //create bears bots of normal difficulty
                CreateBots(bearsNormal, EPlayerSide.Bear, sptBear, BotDifficulty.normal, 5);
                CreateBots(usecNormal, EPlayerSide.Usec, sptUsec, BotDifficulty.normal, 5);
                CreateBots(assaultNormal, EPlayerSide.Savage, WildSpawnType.assault, BotDifficulty.normal, 5);
            }
            else if (DonutsPlugin.botDifficulties.Value.ToLower() == "hard")
            {
                //create bears bots of hard difficulty
                CreateBots(bearsHard, EPlayerSide.Bear, sptBear, BotDifficulty.hard, 5);
                CreateBots(usecHard, EPlayerSide.Usec, sptUsec, BotDifficulty.hard, 5);
                CreateBots(assaultHard, EPlayerSide.Savage, WildSpawnType.assault, BotDifficulty.hard, 5);
                
            }
            else if (DonutsPlugin.botDifficulties.Value.ToLower() == "impossible")
            {
                //create bears bots of impossible difficulty

                CreateBots(bearsImpossible, EPlayerSide.Bear, sptBear, BotDifficulty.impossible, 5);
                CreateBots(usecImpossible, EPlayerSide.Usec, sptUsec, BotDifficulty.impossible, 5);
                CreateBots(assaultImpossible, EPlayerSide.Savage, WildSpawnType.assault, BotDifficulty.impossible, 5);
            }

        }
        private void Update()
        {
            timeSinceLastReplenish += Time.deltaTime;

            if (timeSinceLastReplenish >= replenishInterval)
            {
                timeSinceLastReplenish = 0f;

                ReplenishBots(bearsEasy, EPlayerSide.Bear, sptBear, BotDifficulty.easy);
                ReplenishBots(usecEasy, EPlayerSide.Usec, sptUsec, BotDifficulty.easy);
                ReplenishBots(assaultEasy, EPlayerSide.Savage, WildSpawnType.assault, BotDifficulty.easy);

                ReplenishBots(bearsNormal, EPlayerSide.Bear, sptBear, BotDifficulty.normal);
                ReplenishBots(usecNormal, EPlayerSide.Usec, sptUsec, BotDifficulty.normal);
                ReplenishBots(assaultNormal, EPlayerSide.Savage, WildSpawnType.assault, BotDifficulty.normal);

                ReplenishBots(bearsHard, EPlayerSide.Bear, sptBear, BotDifficulty.hard);
                ReplenishBots(usecHard, EPlayerSide.Usec, sptUsec, BotDifficulty.hard);
                ReplenishBots(assaultHard, EPlayerSide.Savage, WildSpawnType.assault, BotDifficulty.hard);

                ReplenishBots(bearsImpossible, EPlayerSide.Bear, sptBear, BotDifficulty.impossible);
                ReplenishBots(usecImpossible, EPlayerSide.Usec, sptUsec, BotDifficulty.impossible);
                ReplenishBots(assaultImpossible, EPlayerSide.Savage, WildSpawnType.assault, BotDifficulty.impossible);
            }
        }

        private async Task ReplenishBots(List<GClass628> botList, EPlayerSide side, WildSpawnType spawnType, BotDifficulty difficulty)
        {
            int currentCount = botList.Count;
            int botsToAdd = maxBotCount - currentCount;

            if (botsToAdd > 0)
            {
                await CreateBots(botList, side, spawnType, difficulty, botsToAdd);
            }
        }

        private async Task CreateBots(List<GClass628> botList, EPlayerSide side, WildSpawnType spawnType, BotDifficulty difficulty, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                IBotData botData = new GClass629(side, spawnType, difficulty, 0f, null);
                var bot = await GClass628.Create(botData, botCreator, 1, botSpawnerClass);
                botList.Add(bot);
            }
        }
        internal static List<GClass628> GetWildSpawnData(WildSpawnType spawnType, BotDifficulty botDifficulty)
        {
            switch (botDifficulty)
            {
                case BotDifficulty.easy:
                    if(spawnType == WildSpawnType.assault)
                    {
                        return assaultEasy;
                    }
                    else if (spawnType == sptUsec)
                    {
                        return usecEasy;
                    }
                    else
                    {
                        return bearsEasy;
                    }

                case BotDifficulty.normal:
                    if (spawnType == WildSpawnType.assault)
                    {
                        return assaultNormal;
                    }
                    else if (spawnType == sptUsec)
                    {
                        return usecNormal;
                    }
                    else
                    {
                        return bearsNormal;
                    }

                case BotDifficulty.hard:
                    if (spawnType == WildSpawnType.assault)
                    {
                        return assaultHard;
                    }
                    else if (spawnType == sptUsec)
                    {
                        return usecHard;
                    }
                    else
                    {
                        return bearsHard;
                    }

                case BotDifficulty.impossible:
                    if (spawnType == WildSpawnType.assault)
                    {
                        return assaultImpossible;
                    }
                    else if (spawnType == sptUsec)
                    {
                        return usecImpossible;
                    }
                    else
                    {
                        return bearsImpossible;
                    }

                default:
                    return null;
            }
            
            
        }

    }
}