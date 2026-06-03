
using System;
using System.Reflection;
using DrakiaXYZ.BigBrain.Brains;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.SAIN
{
    /// <summary>
    /// 让SAIN的CombatSoloLayer层级激活时也执行特定的代码
    /// </summary>
    public class CombatSoloLayerStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(Type.GetType("SAIN.Layers.Combat.Solo.CombatSoloLayer, SAIN"), "Start");

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(CustomLayer __instance)
        {
            if (McsMgr.IsMcsBotPlayer(__instance.BotOwner.ProfileId))
            {
                var mcsBotPlayerData = __instance.BotOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData == null)
                {
                    return;
                }

                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.ShouldHoldPosition = false;
                    mcsBotPlayerData.ShouldGoToPoint = false;
                    mcsBotPlayerData.IsLooting = false;
                }

                __instance.BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.OnFirstContact,
                    Position = __instance.BotOwner.Memory.GoalEnemy.EnemyLastPosition
                });
            }
        }
    }
}