using System;
using System.Collections.Generic;
using SPT.PrePatch;
using Cysharp.Threading.Tasks;
using EFT;
using static Donuts.DonutComponent;
using System.Threading;
using System.Linq;

#pragma warning disable IDE0007, IDE0044
namespace Donuts
{
    public static class BotCountManager
    {
        private static readonly HashSet<WildSpawnType> BossTypes = new HashSet<WildSpawnType>
        {
            WildSpawnType.arenaFighterEvent, WildSpawnType.bossBoar, WildSpawnType.bossBoarSniper,
            WildSpawnType.bossBully, WildSpawnType.bossGluhar, WildSpawnType.bossKilla,
            WildSpawnType.bossKojaniy, WildSpawnType.bossKolontay, WildSpawnType.bossSanitar,
            WildSpawnType.bossTagilla, WildSpawnType.bossZryachiy, WildSpawnType.crazyAssaultEvent,
            WildSpawnType.exUsec, WildSpawnType.followerBoar, WildSpawnType.followerBully,
            WildSpawnType.followerGluharAssault, WildSpawnType.followerGluharScout,
            WildSpawnType.followerGluharSecurity, WildSpawnType.followerGluharSnipe,
            WildSpawnType.followerKojaniy, WildSpawnType.followerKolontaySecurity,
            WildSpawnType.followerKolontayAssault, WildSpawnType.followerSanitar,
            WildSpawnType.followerTagilla, WildSpawnType.followerZryachiy, WildSpawnType.gifter,
            WildSpawnType.marksman, WildSpawnType.pmcBot, WildSpawnType.sectantPriest,
            WildSpawnType.sectantWarrior, WildSpawnType.followerBigPipe, WildSpawnType.followerBirdEye,
            WildSpawnType.bossKnight
        };

        public static UniTask<int> GetAlivePlayers(string spawnType, CancellationToken cancellationToken)
        {
            return UniTask.Create(async () =>
            {
                int count = 0;
                foreach (Player bot in gameWorld.AllAlivePlayersList)
                {
                    if (!bot.IsYourPlayer)
                    {
                        switch (spawnType)
                        {
                            case "scav":
                                if (IsSCAV(bot.Profile.Info.Settings.Role))
                                {
                                    count++;
                                }
                                break;
                            case "pmc":
                                if (IsPMC(bot.Profile.Info.Settings.Role))
                                {
                                    count++;
                                }
                                break;
                            case "boss":
                                if (IsBoss(bot.Profile.Info.Settings.Role))
                                {
                                    count++;
                                }
                                break;
                            default:
                                throw new ArgumentException("Invalid spawnType", nameof(spawnType));
                        }
                    }
                }
                return count;
            });
        }

        public static bool IsPMC(WildSpawnType role)
        {
            return role == WildSpawnType.pmcUSEC || role == WildSpawnType.pmcBEAR;
        }

        public static bool IsSCAV(WildSpawnType role)
        {
            return role == WildSpawnType.assault;
        }

        public static bool IsBoss(WildSpawnType role)
        {
            return BossTypes.Contains(role);
        }
    }
}
