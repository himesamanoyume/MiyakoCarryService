using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Patches.BigSurvey;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    internal sealed class LogBuffer
    {
        /// <summary>
        /// 按对局保留的 Brain/Layer/Reason 采样记录（跨对局不清空，分开显示）
        /// </summary>
        internal sealed class RaidUsedInfo
        {
            public int RaidIndex;
            public string LocationId;

            /// <summary>
            /// 绑定的 GameWorld 实例 ID（脚本引擎 raid 中重载时同局延续，不重复建记录）
            /// </summary>
            public int GameWorldInstanceId;

            public ConcurrentDictionary<string, int> UsedBrains = new();
            public ConcurrentDictionary<string, int> UsedLayers = new();
            public ConcurrentDictionary<string, int> UsedReasons = new();
        }

        /// <summary>
        /// 保留的最大对局记录数（超限丢最旧，防内存无限增长）
        /// </summary>
        private const int MAX_RAID_RECORDS = 8;

        private readonly ConcurrentDictionary<string, LogEntry> _entries = new();
        private int _newBigSurveyCount = 0;
        private readonly object _raidRecordsLock = new();
        private readonly List<RaidUsedInfo> _raidRecords = new();
        private int _raidIndexCounter = 0;
        private RaidUsedInfo _currentRaidRecord = null;

        public ConcurrentDictionary<string, LogEntry> GetEntries()
        {
            return _entries;
        }

        public int GetLogCount
        {
            get
            {
                return _entries.Count;
            }
        }

        /// <summary>
        /// 开启一条新对局记录（旧的保留用于分局显示）。幂等：同一 GameWorld 实例不重复创建
        /// （脚本引擎 raid 中重载后新协程发现同局则延续，数据不分裂）。
        /// </summary>
        public void BeginRaid(int gameWorldInstanceId)
        {
            lock (_raidRecordsLock)
            {
                if (_currentRaidRecord != null && _currentRaidRecord.GameWorldInstanceId == gameWorldInstanceId)
                {
                    return;
                }

                _raidIndexCounter += 1;
                _currentRaidRecord = new RaidUsedInfo
                {
                    RaidIndex = _raidIndexCounter,
                    LocationId = Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance.LocationId : "Unknown",
                    GameWorldInstanceId = gameWorldInstanceId,
                };
                _raidRecords.Add(_currentRaidRecord);
                if (_raidRecords.Count > MAX_RAID_RECORDS)
                {
                    _raidRecords.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 确保当前对局记录与指定 GameWorld 匹配：相同→延续（返回 true）；不同或无当前记录→创建新记录
        /// （数据驱动创建：由 RecordAgentInfo 检测到有效 Brain 数据时调用——主机执行 Bot 运算才有数据，
        /// 副机永远不触发，天然区分主副机且不受 IsHost 置位时序影响）
        /// </summary>
        public bool EnsureCurrentRaid(int gameWorldInstanceId)
        {
            lock (_raidRecordsLock)
            {
                if (_currentRaidRecord != null && _currentRaidRecord.GameWorldInstanceId == gameWorldInstanceId)
                {
                    return true;
                }
            }

            BeginRaid(gameWorldInstanceId);
            return true;
        }

        public void AddUsedBrain(string brainName)
        {
            if (_currentRaidRecord == null)
            {
                return;
            }

            _currentRaidRecord.UsedBrains.AddOrUpdate(brainName, _ = 1, (brainName, times) =>
            {
                times += 1;
                return times;
            });
        }

        public void AddUsedLayer(string layerName)
        {
            if (_currentRaidRecord == null)
            {
                return;
            }

            _currentRaidRecord.UsedLayers.AddOrUpdate(layerName, _ = 1, (layerName, times) =>
            {
                times += 1;
                return times;
            });
        }

        public void AddUsedReason(string reason)
        {
            if (_currentRaidRecord == null)
            {
                return;
            }

            _currentRaidRecord.UsedReasons.AddOrUpdate(reason, _ = 1, (reason, times) =>
            {
                times += 1;
                return times;
            });
        }

        /// <summary>
        /// 全部对局记录快照（新局在前，供 BigSurvey 分局显示）
        /// </summary>
        public List<RaidUsedInfo> GetRaidUsedInfos()
        {
            lock (_raidRecordsLock)
            {
                var snapshot = new List<RaidUsedInfo>(_raidRecords);
                snapshot.Reverse();
                return snapshot;
            }
        }

        public void AddEntryIfNotFull(string condition, string stackTrace)
        {
            _newBigSurveyCount += 1;

            ShowNewInformation();

            _entries.AddOrUpdate(condition + stackTrace, _ => new LogEntry(condition, stackTrace),
                (key, oldLogEntry) =>
                {
                    oldLogEntry.Total += 1;
                    return oldLogEntry;
                }
            );
        }

        public void ShowNewInformation()
        {
            if (Singleton<PreloaderUI>.Instantiated)
            {
                var menuTaskBar = Singleton<PreloaderUI>.Instance.MenuTaskBar;
                var menuTaskBarTraverse = Traverse.Create(menuTaskBar);
                var newInformations = menuTaskBarTraverse.Field<GameObject[]>("_newInformation").Value;
                foreach (var newInformation in newInformations)
                {
                    newInformation.SetActive(true);
                }

                MenuTaskBarAwakePatch.NewBigSurveyCount += _newBigSurveyCount;
                _newBigSurveyCount = 0;
            }
        }
    }

    public sealed class LogEntry
    {
        public string Condition;
        public string StackTrace;
        public int Total = 1;

        public LogEntry(string condition, string stackTrace)
        {
            Condition = condition;
            StackTrace = stackTrace;
        }
    }
}
