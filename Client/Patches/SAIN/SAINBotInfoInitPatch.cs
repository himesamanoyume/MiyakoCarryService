
using System;
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.SAIN
{
    /// <summary>
    /// 让护航在SAIN环境下也能同步护航级别强度属性
    /// </summary>
    public sealed class SAINBotInfoInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(Type.GetType("SAIN.SAINComponent.Classes.Info.SAINBotInfoClass, SAIN"), "Init");

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(object __instance)
        {
            var botOwner = SAINUtils.GetBotOwnerFromSainBotInfo(__instance);
            if (botOwner == null)
            {
                return;
            }

            if (!McsMgr.IsMcsBotPlayer(botOwner.ProfileId))
            {
                return;
            }

            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            var leadPlayer = mcsBotPlayerData?.LeadPlayer;
            if (leadPlayer == null)
            {
                return;
            }

            var carryServiceLevel = BotSettingUtils.GetCarryServiceLevel(botOwner.GetPlayer.Profile.Info.Level);
            GameLoop.Instance.ApplyMcsBotFileSettings(botOwner.Settings, botOwner, leadPlayer, carryServiceLevel);
        }
    }
}