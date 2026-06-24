

using System.Collections.Generic;
using Comfort.Common;
using EFT;
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
        public static bool IsPlayerInventory(string stringTemplateId)
        {
            return stringTemplateId == CommonId.DefaultInventory;
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
                EItemType.Equipment => lootData.Item is HeadphonesItemClass ? blockItemType.HasFlag(EBlockItemType.Headphone) : blockItemType.HasFlag(EBlockItemType.TacticalVest),
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

        public static List<ItemData> GetRangeOwnerItemData(Vector3 mcsBotPlayerPos, float distance)
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

                if (itemData.RootTransform.position.McsSqrDistance(mcsBotPlayerPos) >= distance * distance)
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
            var xOffset = GClass856.Random(1f, 4f) * GClass856.RandomSing();
            var zOffset = GClass856.Random(1f, 4f) * GClass856.RandomSing();
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
    }
}