

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class PlayerDataMgr : DataMgr<PlayerDataMgr>
    {
        public List<McsBotPlayerData> GetMcsBotPlayerDatas()
        {
            var result = new List<McsBotPlayerData>();
            foreach (var baseData in _datas)
            {
                if (baseData is McsBotPlayerData mcsBotPlayerData)
                {
                    result.Add(mcsBotPlayerData);
                }
            }
            return result;
        }

        public sealed override void Start()
        {
            base.Start();
        }

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            StartCoroutine(ReloadDataLoop(1f));
            StartCoroutine(LoadLootData(1f));
            StartCoroutine(RefreshMcsBotPlayersInterestingLoop(10f));
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
        }

        private IEnumerator RefreshMcsBotPlayersInterestingLoop(float time)
        {
            var waitTime = new WaitForSeconds(time);
            while (true)
            {
                yield return waitTime;
                if (_gameloop.IsVaildGameWorld)
                {
                    var mcsBotPlayerDatas = GetMcsBotPlayerDatas();
                    var closeOwnerItemDatas = new List<ItemData>();
                    foreach (var mcsBotPlayerData in mcsBotPlayerDatas)
                    {
                        closeOwnerItemDatas.AddRange(Tools.GetRangeOwnerItemData(mcsBotPlayerData.Transform.position, 50f));
                    }

                    foreach (var itemData in closeOwnerItemDatas)
                    {
                        itemData.ItemsInContainer = itemData.Item.GetAllDatas().ToList();
                        foreach (var mcsBotPlayerData in mcsBotPlayerDatas)
                        {
                            StartCoroutine(itemData.RefreshRootItemInteresting(mcsBotPlayerData.McsAIBossPlayer));
                        }
                    }
                }
            }
        }

        protected override IEnumerator ReloadDataLoop(float time)
        {
            var waitTime = new WaitForSeconds(time);
            while (true)
            {
                yield return waitTime;

                if (_gameloop.IsVaildGameWorld)
                {
                    var datas = new HashSet<PlayerData>();
                    foreach (var item in Tools.GetAllOwnerItemData())
                    {
                        if (item is PlayerData playerData)
                        {
                            datas.Add(playerData);
                        }
                    }
                    var playerLeft = _datas.Except(datas).ToList();
                    var playerJoined = datas.Except(_datas).ToList();
                    foreach (var playerData in playerLeft)
                    {
                        _datas.Remove(playerData);
                    }
                    foreach (var playerData in playerJoined)
                    {
                        _datas.Add(playerData);
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
            var internalTime = new WaitForSeconds(.2f);
            if (_gameloop.IsVaildGameWorld)
            {
                var datasList = new List<BaseData>();
                datasList.AddRange(_datas);
                int batchSize = Mathf.Clamp(Mathf.CeilToInt(_datas.Count / 10f), 8, 50);
                var playerBatches = new List<List<BaseData>>();
                for (int i = 0; i < _datas.Count; i += batchSize)
                {
                    int endIndex = Math.Min(i + batchSize, _datas.Count);
                    var batch = datasList.GetRange(i, endIndex - i);
                    playerBatches.Add(batch);
                }

                foreach (var batch in playerBatches)
                {
                    try
                    {
                        foreach (PlayerData playerData in batch)
                        {
                            foreach (var mcsAIBossPlayer in SquadMgr.GetAllMcsAIBossPlayer())
                            {
                                playerData.RefreshInteresting(mcsAIBossPlayer);
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