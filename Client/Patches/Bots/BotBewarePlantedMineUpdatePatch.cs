using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 使护航不会拆除玩家布设的绊雷
    /// </summary>
    public class BotBewarePlantedMineUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotBewarePlantedMine), nameof(BotBewarePlantedMine.Update));

        [PatchPrefix]
        public static bool Prefix(BotBewarePlantedMine __instance)
        {
            if (!__instance.BotOwner_0.IsMcsBotPlayer)
            {
                return true;
            }

            if (__instance.NextCheck > Time.time)
            {
                return false;
            }
            var nearestMines = __instance.BotOwner_0.VoxelesPersonalData.CurVoxel.GetNearestMines();
            if (nearestMines.Count == 0)
            {
                return false;
            }
            if (!__instance.BotOwner_0.HasPathAndNotComplete)
            {
                __instance.NextCheck = Time.time + 0.5f;
                return false;
            }
            var max = float.MaxValue;
            var min = float.MinValue;
            PlantedMineAIInfo plantedMineAIInfo = null;
            PlantedMineAIInfo plantedMineAIInfo2 = null;
            var list = new List<Vector3>();
            foreach (var nearestMine in nearestMines)
            {
                var tripwireData = nearestMine.MineObject.GetData();
                if (tripwireData == null || !tripwireData.IsPlantedByAI)
                {
                    continue;
                }

                var vector = nearestMine.Pos - __instance.BotOwner_0.Position;
                if (Vector3.Dot(vector, __instance.BotOwner_0.LookDirection) >= 0f && Mathf.Abs(vector.y) <= 1f)
                {
                    float sqrMagnitude = vector.sqrMagnitude;
                    if (sqrMagnitude < max)
                    {
                        max = sqrMagnitude;
                        plantedMineAIInfo = nearestMine;
                    }
                    if (sqrMagnitude > min)
                    {
                        min = sqrMagnitude;
                        plantedMineAIInfo2 = nearestMine;
                    }
                    list.Add(nearestMine.Pos);
                }
            }
            if (plantedMineAIInfo == null)
            {
                return false;
            }
            var vector2 = (plantedMineAIInfo.Pos + plantedMineAIInfo2.Pos) * 0.5f;
            BotCurrentPathAbstractClass botCurrentPathAbstractClass;
            if (__instance.BotOwner_0.Mover.TryRelacePathAround(vector2, list, out botCurrentPathAbstractClass))
            {
                __instance.NextCheck = Time.time + 4f;
                return false;
            }
            if (plantedMineAIInfo.CanTakeToDeactivate(__instance.BotOwner_0.Id) && __instance.BotOwner_0.Mover.IsPointOnCurrentWay(plantedMineAIInfo.Pos, 2.5f))
            {
                __instance.BotOwner_0.BewarePlantedMine.SetMineToDeactivate(plantedMineAIInfo);
            }
            __instance.NextCheck = Time.time + 2f;
            return false;
        }
    }
}