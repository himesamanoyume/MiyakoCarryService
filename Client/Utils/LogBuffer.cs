using System.Collections.Concurrent;
using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Patches.BigSurvey;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    internal sealed class LogBuffer
    {
        private readonly ConcurrentDictionary<string, LogEntry> _entries = new();
        private int _newBigSurveyCount = 0;
        private readonly ConcurrentDictionary<string, int> _usedBrains = new();
        private readonly ConcurrentDictionary<string, int> _usedLayers = new();
        private readonly ConcurrentDictionary<string, int> _usedReasons = new();

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

        public void AddUsedBrain(string brainName)
        {
            _usedBrains.AddOrUpdate(brainName, _ = 1, (brainName, times) =>
            {
                times += 1;
                return times;
            });
        }

        public void AddUsedLayer(string layerName)
        {
            _usedLayers.AddOrUpdate(layerName, _ = 1, (layerName, times) =>
            {
                times += 1;
                return times;
            });
        }

        public void AddUsedReason(string reason)
        {
            _usedReasons.AddOrUpdate(reason, _ = 1, (reason, times) =>
            {
                times += 1;
                return times;
            });
        }

        public ConcurrentDictionary<string, int> GetUsedBrains()
        {
            return _usedBrains;
        }

        public ConcurrentDictionary<string, int> GetUsedLayers()
        {
            return _usedLayers;
        }

        public ConcurrentDictionary<string, int> GetUsedReasons()
        {
            return _usedReasons;
        }

        public void AddEntryIfNotFull(string condition, string stackTrace)
        {
            _newBigSurveyCount += 1;

            ShowNewInformation();

            _entries.AddOrUpdate(stackTrace, _ => new LogEntry(condition, stackTrace),
                (stackTrace, oldLogEntry) =>
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
