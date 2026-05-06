using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
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
    public sealed class TransitPointPatch1 : ModulePatch
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

    /// <summary>
    /// 修复当护航队友进队时，会因为触发组队转移相关检测而导致转移的问题
    /// </summary>
    public sealed class TransitPointPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TransitPoint), nameof(TransitPoint.method_6));

        private static McsMgr McsMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

        [PatchPrefix]
        public static bool Prefix(Dictionary<string, string> ___dictionary_0, string groupId, out HashSet<string> singlePlayers, out HashSet<string> partyPlayers, out bool partyIsFull, ref HashSet<string> __result)
        {
            var hashSet = new HashSet<string>();
            foreach (var kvp in ___dictionary_0)
            {
                kvp.Deconstruct(out var key, out var value);
                if (value.Equals(groupId))
                {
                    hashSet.Add(key);
                }
            }

            singlePlayers = [.. hashSet];
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