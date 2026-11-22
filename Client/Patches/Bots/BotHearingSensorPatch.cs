using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 借鉴friendlyPmc
    /// </summary>
    public sealed class BotHearingSensorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotHearingSensor), nameof(BotHearingSensor.OnSoundPlayed));

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static void Prefix(BotHearingSensor __instance, IPlayer player, Vector3 position, float power, AISoundType type)
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

                    // 中立角色（俄军/平民）：听到声音不等于敌对，禁止由此主动开战。
                    if (player.IsAI && player.Profile?.Info?.Settings != null && Tools.IsNeutralBot(player.Profile.Info.Settings.Role))
                    {
                        return;
                    }

                    var shouldReact = __instance.IsSoundHeard(position, power, type, out var distance);

                    var enemy = Singleton<GameWorld>.Instance.GetEverExistedPlayerByProfileId(player.ProfileId);
                    if (enemy != null && shouldReact)
                    {
                        if (Tools.TryMakeRoomForEnemy(thisBotOwner.BotsGroup))
                        {
                            thisBotOwner.BotsGroup.AddEnemy(enemy);
                            var mcsLeadPlayer = McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(thisBotOwner.ProfileId);
                            var mcsAILeadPlayer = McsMgr.GetMcsAILeadPlayerByMcsLeadPlayerId(mcsLeadPlayer.ProfileId);
                            mcsAILeadPlayer.CalcGoalEnemy(enemy);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                McsLogger.LogError($"BotHearingSensorPatch 报错: {e}");
            }
        }
    }
}