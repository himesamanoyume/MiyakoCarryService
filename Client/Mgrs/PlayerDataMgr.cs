

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EFT;
using MiyakoCarryService.Client.Datas;
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
            // StartCoroutine(CheckMcsLeadPlayerSeenEnemiesLoop(2f));
            var mcsBotPlayerDatas = GetMcsBotPlayerDatas();
            foreach (var mcsBotPlayerData in mcsBotPlayerDatas)
            {
                mcsBotPlayerData.Player.AIData.BotOwner.Memory.GoalTarget.Clear();
                mcsBotPlayerData.Player.AIData.BotOwner.Memory.GoalEnemy = null;
            }
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
        }

        private IEnumerator RefreshMcsBotPlayersInterestingLoop(float time)
        {
            var waitTime = new WaitForSeconds(time);
            var internalTime = new WaitForSeconds(.2f);
            while (true)
            {
                yield return waitTime;
                if (_gameloop.IsVaildGameWorld)
                {
                    // 收集护航周围的根战利品信息
                    var mcsBotPlayerDatas = GetMcsBotPlayerDatas();
                    var closeRootItemDataDict = new Dictionary<McsBotPlayerData, List<ItemData>>();
                    foreach (var mcsBotPlayerData in mcsBotPlayerDatas)
                    {
                        if (mcsBotPlayerData.RootTransform == null)
                        {
                            continue;
                        }

                        // 若当前有掠夺目标，则不进行获取新的掠夺目标
                        if (mcsBotPlayerData.LootingTarget != null)
                        {
                            continue;
                        }

                        closeRootItemDataDict[mcsBotPlayerData] = Tools.GetRangeOwnerItemData(mcsBotPlayerData.RootTransform.position, 50f);
                    }

                    // 收集分批所需数据
                    var totalRootItemCount = 0;
                    foreach (var list in closeRootItemDataDict.Values)
                    {
                        totalRootItemCount += list.Count;
                    }

                    var totalRootItemDatas = new List<ItemData>();
                    foreach (var list in closeRootItemDataDict.Values)
                    {
                        totalRootItemDatas.AddRange(list);
                    }

                    // 为各批次填充数据
                    var batchSize = Mathf.Clamp(Mathf.CeilToInt(totalRootItemCount / 10f), 100, 2000);
                    var itemBatches = new List<List<ItemData>>();

                    for(int i = 0; i < totalRootItemCount; i += batchSize)
                    {
                        var batch = new List<ItemData>();
                        int endIndex = Math.Min(i + batchSize, totalRootItemCount);
                        for (int j = i; j < endIndex; j++)
                        {
                            batch.Add(totalRootItemDatas[j]);
                        }
                        itemBatches.Add(batch);
                    }

                    // 分批次、依据每位老板的设置进行刷新
                    var mcsAILeadPlayers = McsMgr.GetAllMcsAILeadPlayer();
                    foreach (var batch in itemBatches)
                    {
                        foreach (var rootItemData in batch)
                        {
                            foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                            {
                                rootItemData.RefreshInteresting(mcsAILeadPlayer);
                            }
                        }
                        yield return internalTime;
                    }

                    // 让每位护航都获取到当前范围内未被锁定的最高优先级的战利品
                    foreach (var keyValuePair in closeRootItemDataDict)
                    {
                        var mcsBotPlayerData = keyValuePair.Key;
                        var closeRootItemDatas = keyValuePair.Value;

                        var closeAllLootData = new List<ItemData>();
                        foreach (var closeRootItem in closeRootItemDatas)
                        {
                            closeAllLootData.AddRange(closeRootItem.ItemsInContainer);
                        }

                        mcsBotPlayerData.SetLootingTarget(closeAllLootData);
                        yield return internalTime;
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
                            foreach (var mcsAILeadPlayer in McsMgr.GetAllMcsAILeadPlayer())
                            {
                                playerData.RefreshInteresting(mcsAILeadPlayer);
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

        private IEnumerator CheckMcsLeadPlayerSeenEnemiesLoop(float time)
        {
            var waitTime = new WaitForSeconds(time);
            var internalTime = new WaitForSeconds(.2f);
            while (true)
            {
                yield return waitTime;
                if (_gameloop.IsVaildGameWorld)
                {
                    var mcsAILeadPlayers = McsMgr.GetAllMcsAILeadPlayer();
                    foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                    {
                        var leadPlayer = mcsAILeadPlayer.Player() as Player;
                        var leadPlayerPos = leadPlayer.Position + Vector3.up * 1.6f;
                        var playerDatas = GetDatas<PlayerData>();
                        foreach (var playerData in playerDatas)
                        {
                            if (playerData.Player.IsAI && !McsMgr.IsMcsBotPlayer(playerData.Player.ProfileId))
                            {
                                continue;
                            }

                            if (!playerData.Player.IsAI)
                            {
                                continue;
                            }

                            var angle = Vector3.Angle(leadPlayer.LookDirection, playerData.Player.Position);
                            if (angle > 45f)
                            {
                                continue;
                            }

                            var blocked = Physics.Linecast(
                                leadPlayerPos,
                                playerData.Player.Position + Vector3.up * 1.6f,
                                out var raycastHit, 
                                LayerMaskClass.HighPolyWithTerrainMask
                            );

                            if (!blocked)
                            {
                                // MiyakoCarryServicePlugin.Logger.LogWarning("观测到玩家目视的敌人");
                                mcsAILeadPlayer.CalcGoalEnemy();
                                break;
                            }
                        }

                        yield return internalTime;
                    }
                }
                else
                {
                    yield return null;
                    continue;
                }
            }
        }
    }
}