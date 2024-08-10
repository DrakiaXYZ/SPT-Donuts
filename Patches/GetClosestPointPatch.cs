using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace dvize.Donuts.Patches
{
    internal class GetClosestPointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Targeting the GetClosestPoint method in GClass312
            return AccessTools.Method(typeof(GClass312), nameof(GClass312.GetClosestPoint));
        }

        [PatchPrefix]
        public static bool Prefix(GClass312 __instance, BotOwner bot, Vector3 pos, bool noRestrictions, Func<GroupPoint, bool> goodFunc, bool printErrorLogsIfFail, int maxIterations, 
            ref CustomNavigationPoint __result, GClass300<GroupPointSearchData> ___gclass300_0, HashSet<int> ___hashSet_0, StringBuilder ___stringBuilder_0)
        {

                if (bot == null)
                {
                    //Debug.LogError("GetClosestPointPatch: BotOwner is null, aborting.");
                    __result = null;
                    return false;
                }

                if (bot.VoxelesPersonalData == null)
                {
                    //Debug.LogError("GetClosestPointPatch: BotOwner.VoxelesPersonalData is null, aborting.");
                    __result = null;
                    return false;
                }

                // Original logic
                bot.VoxelesPersonalData.GetVoxelSafe(pos, false);
                int connectionGroupId = bot.StartCorePoint?.ConnectionGroupId ?? -1;

                if (connectionGroupId == -1)
                {
                    //Debug.LogError("GetClosestPointPatch: ConnectionGroupId is invalid, aborting.");
                    __result = null;
                    return false;
                }

                string text;
                GroupPoint startPoint = GClass312.GetStartPoint(bot.CoverSearchInfo, pos, out text);

                if (startPoint == null)
                {
                    //Debug.LogError("GetClosestPointPatch: StartPoint is null, aborting.");
                    __result = null;
                    return false;
                }

                bool flag = GClass330.Instance.IsTraceEnable();
                ___gclass300_0.Clear();
                ___hashSet_0.Clear();
                ___stringBuilder_0 = new StringBuilder();
                int num = 0;

                bool boolFlag = GClass330.Instance.IsTraceEnable() && DebugBotData.UseDebugData && DebugBotData.Instance.DebugCoverLogs;

                if (boolFlag)
                {
                    __instance.method_7(string.Format("botid:{0}  {1}  at start pos:{2}  botPos:{3} ", new object[]
                    {
                        bot.Id,
                        bot.Profile.Info.Settings.Role.ToString(),
                        pos,
                        bot.Position
                    }), false);
                }

                if (goodFunc == null)
                {
                    noRestrictions = true;
                }

                if (startPoint.IsFreeById(bot.Id) && (noRestrictions || goodFunc(startPoint)))
                {
                    CustomNavigationPoint byId = startPoint.GetById(bot.Id);
                    if (flag)
                    {
                        __instance.method_5(pos, bot, byId);
                    }
                    __result = byId;
                    return false;
                }

                CustomNavigationPoint closestPoint;
                if (__instance.method_8(bot, pos, false, goodFunc, startPoint, maxIterations, out closestPoint, ref num))
                {
                    if (flag)
                    {
                        __instance.method_5(pos, bot, closestPoint);
                    }
                    __result = closestPoint;
                    return false;
                }

                if (!noRestrictions)
                {
                    CustomNavigationPoint closestPointFallback = __instance.GetClosestPoint(bot, pos, true, null, false, 1000);
                    if (closestPointFallback != null)
                    {
                        if (flag)
                        {
                            __instance.method_5(pos, bot, closestPointFallback);
                        }
                        __result = closestPointFallback;
                        return false;
                    }
                }

                __result = null;
                return false;
            }

        }
    }

