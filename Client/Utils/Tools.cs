

using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Counters;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Utils
{
    public static class Tools
    {
        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        public static bool IsHost => McsMgr.IsHost;

        public static bool IsPlayerInventory(string stringTemplateId)
        {
            return stringTemplateId == ItemTpl.DefaultInventory;
        }

        public static bool IsBlockItem(EBlockItemType blockItemType, LootData lootData)
        {
            var itemType = lootData.ItemType;
            if (blockItemType == 0)
            {
                return false;
            }

            if (blockItemType.HasFlag(EBlockItemType.All))
            {
                return true;
            }

            if (blockItemType.HasFlag(EBlockItemType.Other) && (itemType == EItemType.Special || itemType == EItemType.All || itemType == EItemType.None))
            {
                return true;
            }

            return itemType switch
            {
                EItemType.Ammo => blockItemType.HasFlag(EBlockItemType.Ammo),
                EItemType.Barter => blockItemType.HasFlag(EBlockItemType.Barter),
                EItemType.Container => blockItemType.HasFlag(EBlockItemType.Container),
                EItemType.Food => blockItemType.HasFlag(EBlockItemType.Food),
                EItemType.Backpack => blockItemType.HasFlag(EBlockItemType.Backpack),
                EItemType.Goggles => blockItemType.HasFlag(EBlockItemType.Goggles),
                EItemType.Rig => blockItemType.HasFlag(EBlockItemType.Rig),
                EItemType.Armor => blockItemType.HasFlag(EBlockItemType.Armor),
                EItemType.Equipment => lootData.Item is Headphones ? blockItemType.HasFlag(EBlockItemType.Headphone) : blockItemType.HasFlag(EBlockItemType.TacticalVest),
                EItemType.Grenade => blockItemType.HasFlag(EBlockItemType.Grenade),
                EItemType.Info => blockItemType.HasFlag(EBlockItemType.Info),
                EItemType.Keys => blockItemType.HasFlag(EBlockItemType.Keys),
                EItemType.Knife => blockItemType.HasFlag(EBlockItemType.Knife),
                EItemType.Magazine => blockItemType.HasFlag(EBlockItemType.Magazine),
                EItemType.Meds => blockItemType.HasFlag(EBlockItemType.Meds),
                EItemType.Mod => blockItemType.HasFlag(EBlockItemType.Mod),
                EItemType.Special => blockItemType.HasFlag(EBlockItemType.Special),
                EItemType.Weapon => blockItemType.HasFlag(EBlockItemType.Weapon),
                _ => false
            };
        }

        public static string GetBodyPartTypeLocales(BodyPartType bodyPartType)
        {
            return bodyPartType switch
            {
                BodyPartType.head => Locales.HEAD,
                BodyPartType.body => Locales.BODY,
                BodyPartType.leftArm => Locales.LEFTARM,
                BodyPartType.leftLeg => Locales.LEFTLEG,
                BodyPartType.rightArm => Locales.RIGHTARM,
                BodyPartType.rightLeg => Locales.RIGHTLEG,
                _ => ""
            };
        }

        public static List<ItemData> GetAllOwnerItemData()
        {
            var result = new List<ItemData>();
            var itemOwners = Singleton<GameWorld>.Instance.ItemOwners;

            foreach (var owner in itemOwners)
            {
                var itemData = owner.Key.RootItem.GetData();

                if (itemData != null)
                {
                    result.Add(itemData);
                }
            }

            return result;
        }

        public static List<ItemData> GetRangeOwnerItemData(Vector3 playerPos, float distance)
        {
            var result = new List<ItemData>();
            var itemOwners = Singleton<GameWorld>.Instance.ItemOwners;

            foreach (var owner in itemOwners)
            {
                var itemData = owner.Key.RootItem.GetData();

                if (itemData == null || itemData.RootTransform == null)
                {
                    continue;
                }

                if (itemData.RootTransform.position.McsSqrDistance(playerPos) >= distance * distance)
                {
                    continue;
                }

                if (itemData is PlayerData playerData)
                {
                    if (McsMgr.IsMcsLeadPlayer(playerData.Player.ProfileId) || playerData.Player.HealthController.IsAlive)
                    {
                        continue;
                    }
                }
                result.Add(itemData);
            }
            return result;
        }

        /// <summary>
        /// 借鉴LootingBots
        /// </summary>
        public static bool BetterDestination(float maxDistance, Vector3 destination, out Vector3 betterDestination)
        {
            var pointNearbyContainer = NavMesh.SamplePosition(
                destination,
                out NavMeshHit navMeshAlignedPoint,
                maxDistance,
                NavMesh.AllAreas
            ) ? navMeshAlignedPoint.position : Vector3.zero;

            var padding = destination - pointNearbyContainer;
            padding.y = 0;
            padding.Normalize();

            betterDestination = NavMesh.SamplePosition(
                destination - (padding * 1.5f),
                out navMeshAlignedPoint,
                maxDistance,
                navMeshAlignedPoint.mask
            ) ? navMeshAlignedPoint.position : pointNearbyContainer;

            return betterDestination != Vector3.zero;
        }

        public static Vector3? GetPosNearTarget(Vector3 targetPos, BotOwner botOwner = null)
        {
            Vector3? result = null;
            var xOffset = MyExtensions.Random(1f, 4f) * MyExtensions.RandomSing();
            var zOffset = MyExtensions.Random(1f, 4f) * MyExtensions.RandomSing();
            var newPos = targetPos + new Vector3(xOffset, 0f, zOffset);

            for (int attempt = 0; attempt < 30; attempt++)
            {
                if (BetterDestination(3f, newPos, out var pos))
                {
                    if (Mathf.Abs(pos.y - targetPos.y) <= 2f)
                    {
                        result = pos;
                        break;
                    }
                }
            }

            if (result == null && NavMesh.SamplePosition(newPos, out var navMeshHit, 3f, -1))
            {
                result = navMeshHit.position;
            }

            if (result.HasValue)
            {
                var leadDir = new Vector3();
                if (botOwner != null)
                {
                    leadDir = botOwner.Position - result.Value;
                    leadDir.y = 0;
                    leadDir = leadDir.normalized * 2f;
                }

                if (NavMesh.Raycast(result.Value, leadDir + result.Value, out var rayHit, -1))
                {
                    result = rayHit.position;
                }
                else
                {
                    result = leadDir + result.Value;
                }

                return result;
            }
            return targetPos;
        }

        public static void HandleSharedExperience(Player killedPlayer, IPlayer aggressor, bool fikaSharedKillExp = false, bool fikaSharedBossExp = false)
        {
            if (McsMgr.IsMcsMemberPlayer(aggressor.ProfileId, out var mcsLeadPlayer))
            {
                if (mcsLeadPlayer == null)
                {
                    return;
                }

                if (!mcsLeadPlayer.HealthController.IsAlive)
                {
                    return;
                }

                var settings = killedPlayer.Profile.Info.Settings;
                var countAsBoss = settings.Role.CountAsBossForStatistics() && !(settings.Role is WildSpawnType.pmcUSEC or WildSpawnType.pmcBEAR);
                var experience = settings.Experience;
                var sessionCounters = mcsLeadPlayer.Profile.EftStats.SessionCounters;

                if (experience <= 0)
                {
                    experience = Singleton<GlobalConfiguration>.Instance.Experience.Kill.VictimBotLevelExp;
                }

                if (!countAsBoss)
                {
                    sessionCounters.AddLong(1L, PredefinedCounters.Kills);
                    sessionCounters.AddInt(fikaSharedKillExp ? experience - experience / 2 : experience, PredefinedCounters.ExpKillBase);
                }

                if (countAsBoss)
                {
                    sessionCounters.AddLong(1L, PredefinedCounters.Kills);
                    sessionCounters.AddInt(fikaSharedBossExp ? experience - experience / 2 : experience, PredefinedCounters.ExpKillBase);
                }
            }
        }

        public static void HandleSharedQuestCondition(Player killedPlayer, IPlayer aggressor, DamageInfo damageInfo, EBodyPart bodyPart, bool easyKillConditions = true)
        {
            if (!easyKillConditions)
            {
                return;
            }

            if (McsMgr.IsMcsMemberPlayer(aggressor.ProfileId, out var mcsLeadPlayer))
            {
                if (mcsLeadPlayer == null)
                {
                    return;
                }

                if (!mcsLeadPlayer.HealthController.IsAlive)
                {
                    return;
                }

                var settings = killedPlayer.Profile.Info.Settings;
                var playerSide = killedPlayer.Side;

                if (settings.Role != WildSpawnType.pmcBEAR)
                {
                    if (settings.Role == WildSpawnType.pmcUSEC)
                    {
                        playerSide = EPlayerSide.Usec;
                    }
                }
                else
                {
                    playerSide = EPlayerSide.Bear;
                }

                List<string> list = ["Any"];
                switch (playerSide)
                {
                    case EPlayerSide.Usec:
                        list.Add("Usec");
                        list.Add("AnyPmc");
                        list.Add("Enemy");
                        break;
                    case EPlayerSide.Bear:
                        list.Add("Bear");
                        list.Add("AnyPmc");
                        list.Add("Enemy");
                        break;
                    case EPlayerSide.Savage:
                        list.Add("Savage");
                        list.Add("Bot");
                        break;
                }

                foreach (var target in list)
                {
                    mcsLeadPlayer.QuestController.CheckKillConditionCounter(target, killedPlayer.ProfileId,
                        killedPlayer.Inventory.EquippedInSlotsTemplateIds, damageInfo.Weapon, bodyPart, mcsLeadPlayer.Location,
                        Vector3.Distance(aggressor.Position, killedPlayer.Position), settings.Role.ToStringNoBox(),
                        mcsLeadPlayer.CurrentHour, killedPlayer.HealthController.BodyPartEffects,
                        aggressor.HealthController.BodyPartEffects, killedPlayer.TriggerZones, aggressor.HealthController.ActiveBuffsNames());
                }
            }
        }

        public static List<Vector3> GenerateClearAreaWaypoints(
            Vector3 center, float radius, Vector3 startFrom,
            float cellSize = 4f, float floorHeight = 3f)
        {
            var result = new List<Vector3>();

            var tri = NavMesh.CalculateTriangulation();
            var verts = tri.vertices;
            if (verts == null || verts.Length == 0)
            {
                return result;
            }

            var sqrRadius = radius * radius;

            var inArea = new List<Vector3>();
            foreach (var v in verts)
            {
                var dx = v.x - center.x;
                var dz = v.z - center.z;
                if (dx * dx + dz * dz <= sqrRadius)
                {
                    inArea.Add(v);
                }
            }
            if (inArea.Count == 0)
            {
                return result;
            }

            var buckets = new Dictionary<(int, int, int), Vector3>();
            foreach (var v in inArea)
            {
                var key = (
                    Mathf.FloorToInt(v.x / cellSize),
                    Mathf.FloorToInt(v.y / floorHeight),
                    Mathf.FloorToInt(v.z / cellSize)
                );
                if (!buckets.ContainsKey(key))
                {
                    buckets[key] = v;
                }
            }

            var candidates = new List<Vector3>();
            foreach (var p in buckets.Values)
            {
                if (NavMesh.SamplePosition(p, out var hit, 1.5f, NavMesh.AllAreas))
                {
                    var path = new NavMeshPath();
                    if (NavMesh.CalculatePath(startFrom, hit.position, NavMesh.AllAreas, path)
                        && path.status == NavMeshPathStatus.PathComplete)
                    {
                        candidates.Add(hit.position);
                    }
                }
            }
            if (candidates.Count == 0)
            {
                return result;
            }

            var remaining = new List<Vector3>(candidates);
            var current = startFrom;
            while (remaining.Count > 0)
            {
                int best = 0;
                float bestSqr = float.MaxValue;
                for (int i = 0; i < remaining.Count; i++)
                {
                    float sqr = (remaining[i] - current).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        best = i;
                    }
                }
                current = remaining[best];
                result.Add(current);
                remaining.RemoveAt(best);
            }

            return result;
        }

        public static List<List<Vector3>> SplitRoute(List<Vector3> fullRoute, int count)
        {
            var segments = new List<List<Vector3>>();
            if (count <= 0)
            {
                return segments;
            }

            for (var i = 0; i < count; i++)
            {
                segments.Add(new List<Vector3>());
            }

            if (fullRoute == null || fullRoute.Count == 0)
            {
                return segments;
            }

            var total = fullRoute.Count;
            var baseSize = total / count;
            var remainder = total % count;

            var idx = 0;
            for (int i = 0; i < count; i++)
            {
                var take = baseSize + (i < remainder ? 1 : 0);
                for (var j = 0; j < take && idx < total; j++)
                {
                    segments[i].Add(fullRoute[idx++]);
                }
            }

            return segments;
        }
    }
}