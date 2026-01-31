

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class LootDataMgr : DataMgr<LootDataMgr>
    {
        public HashSet<LootData> LootingTarget = new();
        public sealed override void Start()
        {
            base.Start();
        }

        public bool IsLootingTarget(LootData lootData)
        {
            return LootingTarget.Contains(lootData);
        }

        public void LockLootItemToTarget(LootData lootData)
        {
            LootingTarget.Add(lootData);
        }

        public void UnlockLootingTarget(LootData lootData)
        {
            LootingTarget.Remove(lootData);
        }

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            StartCoroutine(ReloadDataLoop(1f));
            StartCoroutine(LoadLootData(1f));
            LootingTarget.Clear();
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
            LootingTarget.Clear();
        }

        protected override IEnumerator ReloadDataLoop(float time)
        {
            var waitTime = new WaitForSeconds(time);
            while (true)
            {
                yield return waitTime;

                if (_gameloop.IsVaildGameWorld)
                {
                    var datas = new HashSet<LootData>();
                    foreach (var item in Tools.GetAllOwnerItemData())
                    {
                        if (item is LootData lootData)
                        {
                            datas.Add(lootData);
                        }
                    }
                    var lootLeft = _datas.Except(datas).ToList();
                    var lootJoined = datas.Except(_datas).ToList();
                    foreach (var lootData in lootLeft)
                    {
                        _datas.Remove(lootData);
                    }
                    foreach (var lootData in lootJoined)
                    {
                        _datas.Add(lootData);
                    }
                }
                else
                {
                    yield return null;
                    continue;
                }
            }
        }

        protected override IEnumerator LoadLootData(float time)
        {
            yield return new WaitForSeconds(time);
            var internalTime = new WaitForSeconds(.1f);
            if (_gameloop.IsVaildGameWorld)
            {
                var datasList = new List<BaseData>();
                datasList.AddRange(_datas);
                int batchSize = Mathf.Clamp(Mathf.CeilToInt(_datas.Count / 10f), 8, 50);
                var lootBatches = new List<List<BaseData>>();
                for (int i = 0; i < _datas.Count; i += batchSize)
                {
                    int endIndex = Math.Min(i + batchSize, _datas.Count);
                    var batch = datasList.GetRange(i, endIndex - i);
                    lootBatches.Add(batch);
                }

                foreach (var batch in lootBatches)
                {
                    try
                    {
                        foreach (LootData lootData in batch)
                        {
                            foreach (var mcsAIBossPlayer in SquadMgr.GetAllMcsAIBossPlayer())
                            {
                                lootData.RefreshInteresting(mcsAIBossPlayer);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    yield return internalTime;
                }
            }
            else
            {
                yield return null;
            }
        }
    }
}