

using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class LootDataMgr : ItemDataMgr<LootDataMgr>
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
            if (!Tools.IsHost)
            {
                return;
            }
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
    }
}