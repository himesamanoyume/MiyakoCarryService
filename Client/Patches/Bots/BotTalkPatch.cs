using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 在AI发出某些短语时显示字幕
    /// </summary>
    public sealed class BotTalkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotTalk), nameof(BotTalk.method_5));

        private static McsMgr McsMgr
        { 
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

        [PatchPostfix]
        public static void Postfix(BotTalk __instance, EPhraseTrigger type)
        {
            if (!McsMgr.IsHost)
            {
                return;
            }

            var botOwner = __instance.BotOwner_0;

            if (botOwner == null)
            {
                return;
            }

            if (!McsMgr.IsMcsBotPlayer(botOwner.Profile.Id))
            {
                return;
            }

            botOwner.TalkMsg(type);
        }
    }
}