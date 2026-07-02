
using System.Collections.Generic;
using System.Linq;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public class LootDataMgr : ItemDataMgr<LootDataMgr>
    {
        public HashSet<LootData> LockedLootingTarget = new();
        public HashSet<Transform> LockedLootingTargetRootTransform = new();
        public override void Start()
        {
            base.Start();
        }

        public bool IsLockedLootingTarget(LootData lootData)
        {
            return LockedLootingTarget.Contains(lootData);
        }

        public bool IsLockedLootingTargetRootTransform(Transform transform)
        {
            return LockedLootingTargetRootTransform.Contains(transform);
        }

        public void LockLootItemToTarget(LootData lootData)
        {
            LockedLootingTarget.Add(lootData);
        }

        public void LockLootingTargetRootTransform(Transform transform)
        {
            LockedLootingTargetRootTransform.Add(transform);
        }

        public void UnlockLootingTarget(LootData lootData)
        {
            LockedLootingTarget.Remove(lootData);
        }

        public void UnlockLootingTargetRootTransform(Transform transform)
        {
            LockedLootingTargetRootTransform.Remove(transform);
        }

        protected override void OnRaidStarted()
        {
            base.OnRaidStarted();
            StartCoroutine(ReloadDataLoop(1f, LoadItemData<LootData>));
            StartCoroutine(UpdateItemData(1f));
            LockedLootingTarget.Clear();
            LockedLootingTargetRootTransform.Clear();
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
            LockedLootingTarget.Clear();
            LockedLootingTargetRootTransform.Clear();
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            LockedLootingTarget.Clear();
            LockedLootingTargetRootTransform.Clear();
        }

        public LootData GetNearestLootData(Vector3 playerPos, string templateId)
        {
            var itemsInPlayerInventory = new List<ItemData>();
            foreach (var itemData in Tools.GetAllOwnerItemData())
            {
                if (itemData is PlayerData playerData)
                {
                    itemsInPlayerInventory.AddRange(playerData.Item.GetAllDatas());
                }
            }
            var worldItems = _datas.Except(itemsInPlayerInventory).OfType<LootData>().Where(i => i.Item.TemplateId == templateId).ToList();
            if (worldItems.Count > 0)
            {
                worldItems.Sort((a, b) => b.RootTransform.position.McsSqrDistance(playerPos).CompareTo(a.RootTransform.position.McsSqrDistance(playerPos)));
                var targetLootData = worldItems.FirstOrDefault();
                return targetLootData;
            }
            else
            {
                return null;
            }
        }

        public LootData FindLootData(string itemId)
        {
            var itemsInPlayerInventory = new List<ItemData>();
            foreach (var itemData in Tools.GetAllOwnerItemData())
            {
                if (itemData is PlayerData playerData)
                {
                    itemsInPlayerInventory.AddRange(playerData.Item.GetAllDatas());
                }
            }
            var worldItems = _datas.Except(itemsInPlayerInventory).OfType<LootData>().Where(i => i.Item.Id == itemId).ToList();
            if (worldItems.Count > 0)
            {
                var targetLootData = worldItems.FirstOrDefault();
                return targetLootData;
            }
            else
            {
                return null;
            }
        }
    }
}