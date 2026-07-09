using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 借鉴friendlyPmc
    /// </summary>
    public sealed class BotHearingSensorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotHearingSensor), nameof(BotHearingSensor.OnSoundPlayed));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static void Postfix(BotHearingSensor __instance, IPlayer player, Vector3 position, float power, AISoundType type)
        {
            try
            {
                if (!Tools.IsHost)
                {
                    return;
                }

                var thisBotOwner = __instance._botOwner;
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

                    var shouldReact = __instance.IsSoundHeard(position, power, out var distance);

                    var enemy = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(player.ProfileId);
                    if (enemy != null && shouldReact)
                    {
                        thisBotOwner.BotsGroup.ReportAboutEnemy(enemy, EEnemyPartVisibleType.Visible, __instance._botOwner);
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