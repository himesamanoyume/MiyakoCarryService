using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 借鉴friendlyPmc
    /// </summary>
    internal sealed class BotHearingSensorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotHearingSensor), nameof(BotHearingSensor.method_0));

        private static McsMgr McsMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

        [PatchPrefix]
        public static void Postfix(BotHearingSensor __instance, IPlayer player, Vector3 position, float power, AISoundType type)
        {
            try
            {
                var thisBotOwner = __instance.BotOwner;
                if (thisBotOwner == null)
                {
                    return;
                }

                if (McsMgr.IsMcsBotPlayer(thisBotOwner.ProfileId) && player != null && !McsMgr.IsMcsLeadPlayer(player.ProfileId))
                {
                    if (player.IsAI && McsMgr.IsMcsBotPlayer(player.ProfileId))
                    {
                        return;
                    }

                    var shouldReact = __instance.method_6(position, power, out var distance);

                    var enemy = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(player.ProfileId);
                    if (enemy != null && shouldReact)
                    {
                        thisBotOwner.BotsGroup.ReportAboutEnemy(enemy, EEnemyPartVisibleType.Visible, __instance.BotOwner);
                    }
                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError($"BotHearingSensorPatch 报错: {e}");
            }
        }
    }
}