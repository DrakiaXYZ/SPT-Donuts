using System.Collections;
using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.AI;

namespace dvize.Donuts.Patches
{
    //delay setting the patrol for bosses as it causes nullref error on spawn
    internal class PointSettedPatch : ModulePatch
    {
        private static float delayTime = 1f;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PatrollingData), nameof(PatrollingData.PointSetted));
        }

        [PatchPrefix]
        public static bool Prefix(PatrolPointContainer point, int index, PatrollingData __instance)
        {
            // Pause patrol status to prevent navigation during initialization
            if (__instance.Status != PatrolStatus.pause)
            {
                __instance.Status = PatrolStatus.pause;
                Debug.Log($"PointSettedPatch: PatrolStatus set to pause for bot at index {index}.");

                // Start coroutine to reset status after delay
                CoroutineRunner.Instance.StartCoroutine(ResetPatrolStatusAfterDelay(__instance, delayTime));
            }

            return true;
        }

        private static IEnumerator ResetPatrolStatusAfterDelay(PatrollingData instance, float delay)
        {
            yield return new WaitForSeconds(delay);

            // Reset patrol status if it was paused
            if (instance.Status == PatrolStatus.pause)
            {
                instance.Status = PatrolStatus.go;
                Debug.Log("PointSettedPatch: PatrolStatus reset to go.");
            }
        }
    }

    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;
        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("CoroutineRunner").AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }
    }
}
