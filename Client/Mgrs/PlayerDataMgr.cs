

using System;
using System.Collections;
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class PlayerDataMgr : ItemDataMgr<PlayerDataMgr>
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
            StartCoroutine(ReloadDataLoop(1f, LoadItemData<PlayerData>));
            StartCoroutine(UpdateItemData(1f));
            StartCoroutine(RefreshMcsBotPlayersInterestingLoop(10f));
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
            var publicTime = new WaitForSeconds(.2f);
            while (true)
            {
                yield return waitTime;
                if (_gameloop.IsVaildGameWorld)
                {
                    var mcsBotPlayerDatas = GetMcsBotPlayerDatas();
                    var closeRootItemDataDict = new Dictionary<McsBotPlayerData, List<ItemData>>();
                    foreach (var mcsBotPlayerData in mcsBotPlayerDatas)
                    {
                        if (mcsBotPlayerData.RootTransform == null)
                        {
                            continue;
                        }

                        if (mcsBotPlayerData.LootingTarget != null)
                        {
                            continue;
                        }

                        closeRootItemDataDict[mcsBotPlayerData] = Tools.GetRangeOwnerItemData(mcsBotPlayerData.RootTransform.position, 30f);
                    }

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

                    var batchSize = Mathf.Clamp(Mathf.CeilToInt(totalRootItemCount / 10f), 100, 2000);
                    var itemBatches = new List<List<ItemData>>();
                    var batch = new List<ItemData>();

                    for(int i = 0; i < totalRootItemCount; i += batchSize)
                    {
                        batch.Clear();
                        int endIndex = Math.Min(i + batchSize, totalRootItemCount);
                        for (int j = i; j < endIndex; j++)
                        {
                            batch.Add(totalRootItemDatas[j]);
                        }
                        itemBatches.Add(batch);
                    }

                    var mcsAILeadPlayers = McsMgr.GetAllMcsAILeadPlayer();
                    foreach (var _batch in itemBatches)
                    {
                        foreach (var rootItemData in _batch)
                        {
                            foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                            {
                                rootItemData.RefreshInteresting(mcsAILeadPlayer, false);
                            }
                        }
                        yield return publicTime;
                    }

                    foreach (var keyValuePair in closeRootItemDataDict)
                    {
                        var mcsBotPlayerData = keyValuePair.Key;
                        var closeRootItemDatas = keyValuePair.Value;

                        var closeAllLootData = new List<ItemData>();
                        foreach (var closeRootItem in closeRootItemDatas)
                        {
                            if (closeRootItem.ItemsInContainer != null)
                            {
                                closeAllLootData.AddRange(closeRootItem.ItemsInContainer);
                            }
                        }

                        mcsBotPlayerData.SetLootingTarget(closeAllLootData);
                        yield return publicTime;
                    }
                }
            }
        }

        private IEnumerator CheckMcsLeadPlayerSeenEnemiesLoop(float time)
        {
            var waitTime = new WaitForSeconds(time);
            var publicTime = new WaitForSeconds(.2f);
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
                                mcsAILeadPlayer.CalcGoalEnemy();
                                break;
                            }
                        }

                        yield return publicTime;
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