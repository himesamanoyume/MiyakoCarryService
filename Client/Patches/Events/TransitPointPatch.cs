using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.Game.Spawning;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 使转移时护航队友也一起转移
    /// 以后应该会改成需要护航队友实际触发转移
    /// </summary>
    public sealed class TransitPointPatch1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TransitPoint), nameof(TransitPoint.Transit));

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static void Prefix(Il2CppSystem.Collections.Generic.HashSet<string> players)
        {
            foreach (var playerId in players)
            {
                if (McsMgr.IsMcsLeadPlayer(playerId))
                {
                    foreach (var mcsBotPlayer in McsMgr.GetAllMcsSquadMembersByMcsLeadId(playerId))
                    {
                        if (mcsBotPlayer.HealthController.IsAlive)
                        {
                            McsMgr.AddMcsSquadMemberToTransit(playerId, mcsBotPlayer.BotOwner);
                        }
                        else
                        {
                            McsMgr.McsBotPlayerDead(mcsBotPlayer.ProfileId);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 修复当护航队友进队时，会因为触发组队转移相关检测而导致转移的问题
    /// </summary>
    public sealed class TransitPointPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TransitPoint), nameof(TransitPoint.SeparateGroupPlayers));

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static bool Prefix(TransitPoint __instance, string groupId, out Il2CppSystem.Collections.Generic.HashSet<string> singlePlayers, out Il2CppSystem.Collections.Generic.HashSet<string> partyPlayers, out bool partyIsFull, ref Il2CppSystem.Collections.Generic.HashSet<string> __result)
        {
            var hashSet = new Il2CppSystem.Collections.Generic.HashSet<string>();
            foreach ((var key, var value) in __instance._playerGroups)
            {
                if (value.Equals(groupId))
                {
                    hashSet.Add(key);
                }
            }

            singlePlayers = new Il2CppSystem.Collections.Generic.HashSet<string>(hashSet);;
            partyPlayers = new();
            partyIsFull = true;
            foreach (var player in Singleton<GameWorld>.Instance.GroupPlayers(groupId))
            {
                if (player.HealthController.IsAlive)
                {
                    var profileId = player.ProfileId;

                    if (McsMgr.IsMcsBotPlayer(profileId))
                    {
                        continue;
                    }

                    if (!hashSet.Contains(profileId))
                    {
                        partyIsFull = false;
                    }
                    else
                    {
                        singlePlayers.Remove(profileId);
                        partyPlayers.Add(profileId);
                    }
                }
            }

            __result = hashSet;
            return false;
        }
    }
}