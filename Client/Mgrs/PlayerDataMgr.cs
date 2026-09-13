

using System;
using System.Collections;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI.Screens;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public class PlayerDataMgr : ItemDataMgr
    {
        private List<McsBotPlayerData> _visibleDatas = new();

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

        private BrainMgr BrainMgr => field ??= MgrAccessor.Get<BrainMgr>();

        public override void Start()
        {
            base.Start();
            MiyakoCarryServicePlugin.DebugTextSize.SettingChanged += OnDebugTextSizeChanged;
        }

        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            StartCoroutine(ReloadDataLoop(1f, LoadItemData<PlayerData>));
            StartCoroutine(RecordAgentInfo(1f));
            StartCoroutine(UpdateItemData(1f));
            StartCoroutine(RefreshMcsBotPlayersInterestingLoop(10f));
            StartCoroutine(CheckMcsLeadPlayerSeenEnemiesLoop(1f));
            StartCoroutine(CheckFastOpenDoorLoop(1f));
            if (Draw.GuiCommonStyle == null)
            {
                Draw.CreateGuiStyle();
            }
            Draw.GuiCommonStyle.fontSize = MiyakoCarryServicePlugin.DebugTextSize.Value;
            _visibleDatas.Clear();
            StartCoroutine(ReloadDataVisibleLoop(1f));
            StartCoroutine(UpdateDataLoop());
            var mcsBotPlayerDatas = GetMcsBotPlayerDatas();
            foreach (var mcsBotPlayerData in mcsBotPlayerDatas)
            {
                mcsBotPlayerData.Player.AIData.BotOwner.Memory.GoalTarget.Clear();
                mcsBotPlayerData.Player.AIData.BotOwner.Memory.GoalEnemy = null;
            }
        }

        public IEnumerator RecordAgentInfo(float time)
        {
            var waitTime = new WaitForSeconds(time);
            while (true)
            {
                yield return waitTime;
                if (Gameloop.IsVaildGameWorld)
                {
                    try
                    {
                        var mcsBotPlayerDatas = GetMcsBotPlayerDatas();
                        foreach (var mcsBotPlayerData in mcsBotPlayerDatas)
                        {
                            var brain = mcsBotPlayerData.BotOwner?.Brain;
                            var baseBrain = brain?.BaseBrain;
                            if (baseBrain == null)
                            {
                                continue;
                            }

                            if (!MiyakoCarryServicePlugin.LogBuffer.EnsureCurrentRaid(Singleton<GameWorld>.Instance.GetInstanceID()))
                            {
                                continue;
                            }

                            MiyakoCarryServicePlugin.LogBuffer.AddUsedBrain(baseBrain.ShortName());
                            MiyakoCarryServicePlugin.LogBuffer.AddUsedLayer(brain.ActiveLayerName());
                            MiyakoCarryServicePlugin.LogBuffer.AddUsedReason(brain.GetActiveNodeReason());

                            if (!LayerUtils.IsMcsBotPlayerInjected(mcsBotPlayerData.BotOwner))
                            {
                                BrainMgr.InjectLayers(baseBrain);
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }

        private IEnumerator RefreshMcsBotPlayersInterestingLoop(float time)
        {
            var waitTime = new WaitForSeconds(time);
            var publicTime = new WaitForSeconds(.2f);
            while (true)
            {
                yield return waitTime;
                if (Gameloop.IsVaildGameWorld)
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

                    for (int i = 0; i < totalRootItemCount; i += batchSize)
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
                if (Gameloop.IsVaildGameWorld)
                {
                    var mcsAILeadPlayers = McsMgr.GetAllMcsAILeadPlayer();
                    foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                    {
                        if (Time.time >= mcsAILeadPlayer.NextEnemyCleanupTime)
                        {
                            mcsAILeadPlayer.NextEnemyCleanupTime = Time.time + 3f;
                            mcsAILeadPlayer.CleanupDeadEnemies();
                        }

                        var leadPlayer = mcsAILeadPlayer.Player() as Player;
                        var leadPlayerPos = leadPlayer.Position + Vector3.up * 1.6f;
                        var playerDatas = GetDatas<PlayerData>();

                        var leadVisibleEnemies = mcsAILeadPlayer.LeadVisibleEnemies;
                        leadVisibleEnemies.Clear();

                        Player reportTarget = null;
                        var reportSqrDistance = float.MaxValue;

                        Player aimingTarget = null;
                        var aimingSqrDistance = float.MaxValue;

                        foreach (var playerData in playerDatas)
                        {
                            var target = playerData.Player;

                            if (target == null || !target.HealthController.IsAlive)
                            {
                                continue;
                            }

                            if (!target.IsAI)
                            {
                                continue;
                            }

                            if (McsMgr.IsMcsBotPlayer(target.ProfileId))
                            {
                                continue;
                            }

                            var targetBotOwner = target.AIData?.BotOwner;
                            if (targetBotOwner != null && targetBotOwner.BotState != EBotState.NonActive)
                            {
                                var targetGoalEnemy = targetBotOwner.Memory?.GoalEnemy;
                                if (targetGoalEnemy?.Person != null
                                    && targetGoalEnemy.Person.ProfileId == leadPlayer.ProfileId
                                    && (targetGoalEnemy.IsVisible || targetGoalEnemy.CanShoot || targetBotOwner.ShootData?.Shooting == true))
                                {
                                    var aimSqrDistance = target.Position.McsSqrDistance(leadPlayer.Position);
                                    if (aimSqrDistance < 300f * 300f && aimSqrDistance < aimingSqrDistance)
                                    {
                                        aimingTarget = target;
                                        aimingSqrDistance = aimSqrDistance;
                                    }
                                }
                            }

                            if (leadPlayer.Side == EPlayerSide.Savage && target.Side == EPlayerSide.Savage && !leadPlayer.BotsGroup.IsEnemy(target))
                            {
                                continue;
                            }

                            var sqrDistance = target.Position.McsSqrDistance(leadPlayer.Position);
                            if (sqrDistance >= 150f * 150f)
                            {
                                continue;
                            }

                            var dirToTarget = target.Position - leadPlayer.Position;
                            var angle = Vector3.Angle(leadPlayer.LookDirection, dirToTarget);
                            if (angle > 45f)
                            {
                                continue;
                            }

                            var blocked = Physics.Linecast(
                                leadPlayerPos,
                                target.Position + Vector3.up * 1.6f,
                                out var raycastHit,
                                LayersMaskController.HighPolyWithTerrainMask
                            );

                            if (blocked)
                            {
                                continue;
                            }

                            leadVisibleEnemies.Add(target);

                            if (sqrDistance < reportSqrDistance)
                            {
                                reportTarget = target;
                                reportSqrDistance = sqrDistance;
                            }
                        }

                        if (leadVisibleEnemies.Count > 1)
                        {
                            leadVisibleEnemies.Sort((a, b) => a.Position.McsSqrDistance(leadPlayer.Position).CompareTo(b.Position.McsSqrDistance(leadPlayer.Position)));
                        }

                        if (reportTarget != null)
                        {
                            mcsAILeadPlayer.CalcGoalEnemy(reportTarget);
                        }

                        if (aimingTarget != null)
                        {
                            mcsAILeadPlayer.MarkLeadAimingEnemy(aimingTarget);
                            mcsAILeadPlayer.CalcGoalEnemy(aimingTarget);
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

        private IEnumerator CheckFastOpenDoorLoop(float time)
        {
            var waitTime = new WaitForSeconds(time);
            while (true)
            {
                yield return waitTime;
                if (!Gameloop.IsVaildGameWorld)
                {
                    yield return null;
                    continue;
                }

                var now = Time.time;
                var mcsBotPlayerDatas = GetMcsBotPlayerDatas();
                foreach (var mcsBotPlayerData in mcsBotPlayerDatas)
                {
                    if (mcsBotPlayerData == null)
                    {
                        continue;
                    }

                    mcsBotPlayerData.TryFinishFastOpenDoor();

                    if (mcsBotPlayerData.FastOpenDoorCooldowns.Count > 16)
                    {
                        var expiredDoorIds = new List<string>();
                        foreach (var kvp in mcsBotPlayerData.FastOpenDoorCooldowns)
                        {
                            if (kvp.Value < now)
                            {
                                expiredDoorIds.Add(kvp.Key);
                            }
                        }
                        foreach (var expiredDoorId in expiredDoorIds)
                        {
                            mcsBotPlayerData.FastOpenDoorCooldowns.Remove(expiredDoorId);
                        }
                    }
                }
            }
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            _visibleDatas.Clear();
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            MiyakoCarryServicePlugin.DebugTextSize.SettingChanged -= OnDebugTextSizeChanged;
        }

        private void OnDebugTextSizeChanged(object sender, EventArgs e)
        {
            if (Draw.GuiCommonStyle == null)
            {
                return;
            }

            Draw.GuiCommonStyle.fontSize = MiyakoCarryServicePlugin.DebugTextSize.Value;
            foreach (var mcsBotPlayerData in _visibleDatas)
            {
                mcsBotPlayerData.SyncFontSize();
            }
        }

        private IEnumerator ReloadDataVisibleLoop(float time)
        {
            var waitTime = new WaitForSeconds(time);
            while (true)
            {
                yield return waitTime;

                if (!Gameloop.IsVaildGameWorld)
                {
                    yield return null;
                    continue;
                }

                var mcsBotPlayerDatas = GetMcsBotPlayerDatas();
                var visibleDatas = new List<McsBotPlayerData>(mcsBotPlayerDatas.Count);

                foreach (var mcsBotPlayerData in mcsBotPlayerDatas)
                {
                    if (mcsBotPlayerData == null)
                    {
                        continue;
                    }

                    var player = mcsBotPlayerData.Player;
                    if (player == null)
                    {
                        mcsBotPlayerData.IsVisible = false;
                        continue;
                    }

                    if (!player.HealthController.IsAlive)
                    {
                        mcsBotPlayerData.IsVisible = false;
                        continue;
                    }

                    if (player.Transform == null)
                    {
                        mcsBotPlayerData.IsVisible = false;
                        continue;
                    }

                    mcsBotPlayerData.Distance = Mathf.RoundToInt(player.Distance);
                    var distance = mcsBotPlayerData.Distance;

                    if (distance <= 50 || mcsBotPlayerData.IsInCameraView())
                    {
                        mcsBotPlayerData.IsVisible = true;
                    }
                    else
                    {
                        mcsBotPlayerData.IsVisible = false;
                        continue;
                    }

                    if (Tools.IsInvalidCamera())
                    {
                        mcsBotPlayerData.IsVisible = false;
                        continue;
                    }

                    mcsBotPlayerData.UpdateBaseInfo();
                    visibleDatas.Add(mcsBotPlayerData);
                }

                _visibleDatas = visibleDatas;
            }
        }

        private IEnumerator UpdateDataLoop()
        {
            while (true)
            {
                yield return null;
                if (Gameloop.IsVaildGameWorld)
                {
                    foreach (var mcsBotPlayerData in _visibleDatas)
                    {
                        mcsBotPlayerData.UpdateData();
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (Draw.GuiCommonStyle == null)
            {
                Draw.CreateGuiStyle();
            }

            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (!MiyakoCarryServicePlugin.DrawDebugInfo.Value)
            {
                return;
            }

            if (!Gameloop.IsVaildGameWorld)
            {
                return;
            }

            var screenManager = EftScreenManager.Instance;
            if (screenManager == null)
            {
                return;
            }

            var isCursorLocked = Cursor.lockState is CursorLockMode.Locked or CursorLockMode.Confined;
            if (!screenManager.CheckCurrentScreen(EEftScreenType.BattleUI) || !isCursorLocked)
            {
                return;
            }

            if (_visibleDatas.Count == 0)
            {
                return;
            }

            foreach (var mcsBotPlayerData in _visibleDatas)
            {
                if (mcsBotPlayerData == null)
                {
                    continue;
                }

                if (mcsBotPlayerData.BottomScreenPos.x == -10000 && mcsBotPlayerData.BottomScreenPos.y == -10000)
                {
                    continue;
                }

                mcsBotPlayerData.Color = Draw.Green.Rgb;
                GUI.Label(mcsBotPlayerData.Rect, mcsBotPlayerData.Info, mcsBotPlayerData.GUIStyle);
            }
        }
    }
}