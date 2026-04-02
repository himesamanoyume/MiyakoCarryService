using System.Collections.Generic;
using System.Reflection;
using EFT.Interactive;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 使转移时护航队友也一起转移
    /// 以后应该会改成需要护航队友实际触发转移
    /// </summary>
    public sealed class TransitPointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TransitPoint), nameof(TransitPoint.method_7));

        private static McsMgr McsMgr
        { 
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

        [PatchPrefix]
        public static void Prefix(HashSet<string> players)
        {
            foreach (var playerId in players)
            {
                if (McsMgr.IsMcsLeadPlayer(playerId))
                {
                    foreach (var botOwner in McsMgr.GetAllMcsSquadMembersByMcsLeadId(playerId))
                    {
                        if (botOwner.HealthController.IsAlive)
                        {
                            McsMgr.AddMcsSquadMemberToTransit(playerId, botOwner);
                        }
                        else
                        {
                            McsMgr.McsBotPlayerDead(botOwner.ProfileId);
                        }
                    }
                }
            }
        }
    }
}