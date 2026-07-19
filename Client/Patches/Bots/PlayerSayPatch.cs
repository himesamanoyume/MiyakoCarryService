using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 借鉴friendlyPmc
    /// </summary>
    public sealed class PlayerSayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.Say));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(Player __instance, EPhraseTrigger phrase, bool demand = false, float delay = 0f, ETagStatus mask = 0, int probability = 100, bool aggressive = false)
        {
            if (!Tools.IsHost)
            {
                return;
            }

            if (__instance.Profile.Info.GroupId is "Fika" or "Mcs" || McsMgr.IsMcsLeadPlayer(__instance.ProfileId) || McsMgr.IsMcsBotPlayer(__instance.ProfileId))
            {
                return;
            }

            foreach (var mcsBotPlayer in McsMgr.GetAllAliveMcsBotPlayer())
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var pos = __instance?.Transform?.position;
                if (!pos.HasValue)
                {
                    continue;
                }
                if (botOwner.HearingSensor.method_6(pos.Value, 50f, out var dist))
                {
                    botOwner.BotsGroup.AddEnemy(__instance, EBotEnemyCause.callForHelp1);
                    var mcsLeadPlayer = McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(botOwner.ProfileId);
                    var mcsAILeadPlayer = McsMgr.GetMcsAILeadPlayerByMcsLeadPlayerId(mcsLeadPlayer.ProfileId);
                    mcsAILeadPlayer.CalcGoalEnemy(__instance);
                }
            }
        }
    }
}