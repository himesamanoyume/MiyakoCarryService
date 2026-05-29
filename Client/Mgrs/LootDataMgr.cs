

using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class LootDataMgr : DataMgr<LootDataMgr>
    {
        public HashSet<LootData> LockedLootingTarget = new();
        public HashSet<Transform> LockedLootingTargetRootTransform = new();
        public sealed override void Start()
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

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            StartCoroutine(ReloadDataLoop<LootData>(1f));
            StartCoroutine(LoadItemData(1f));
            LockedLootingTarget.Clear();
            LockedLootingTargetRootTransform.Clear();
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
            LockedLootingTarget.Clear();
            LockedLootingTargetRootTransform.Clear();
        }
    }
}