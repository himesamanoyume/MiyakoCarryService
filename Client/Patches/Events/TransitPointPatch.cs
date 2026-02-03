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
    internal sealed class TransitPointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TransitPoint), nameof(TransitPoint.method_7));

        private static SquadMgr SquadMgr
        { 
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPrefix]
        public static void Prefix(HashSet<string> players)
        {
            foreach (var playerId in players)
            {
                if (SquadMgr.IsMcsBossPlayer(playerId))
                {
                    foreach (var botOwner in SquadMgr.GetAllMcsSquadMembersByMcsBossId(playerId))
                    {
                        if (botOwner.HealthController.IsAlive)
                        {
                            SquadMgr.AddMcsSquadMemberToTransit(playerId, botOwner);
                        }
                        else
                        {
                            SquadMgr.McsBotPlayerDead(botOwner.ProfileId);
                        }
                    }
                }
            }
        }
    }
}