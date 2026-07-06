using System.Collections.Generic;
using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Patches.BigSurvey;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    internal sealed class LogBuffer
    {
        private readonly LinkedList<LogEntry> _entries = new();
        private int _currentCharCount = 0;
        private int _newBigSurveyCount = 0;
        private HashSet<string> _usedLayers = new();
        private HashSet<string> _usedNodes = new();
        private HashSet<string> _usedReasons = new();

        public LinkedList<LogEntry> GetEntries()
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

        public void AddUsedLayer(string layerName)
        {
            _usedLayers.Add(layerName);
        }

        public void AddUsedNode(string node)
        {
            _usedNodes.Add(node);
        }

        public void AddUsedReason(string reason)
        {
            _usedReasons.Add(reason);
        }

        public HashSet<string> GetUsedLayers()
        {
            return _usedLayers;
        }

        public HashSet<string> GetUsedNodes()
        {
            return _usedNodes;
        }

        public HashSet<string> GetUsedReasons()
        {
            return _usedReasons;
        }

        public void AddEntryIfNotFull(string condition, string stackTrace)
        {
            _newBigSurveyCount += 1;

            ShowNewInformation();

            var logEntry = new LogEntry(condition, stackTrace);
            var newTotal = _currentCharCount + logEntry.Length;

            _entries.AddLast(logEntry);
            _currentCharCount = newTotal;
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
        public string Condition
        {
            get
            {
                return field;
            }
        }
        public string StackTrace
        {
            get
            {
                return field;
            }
        }
        public int Length
        {
            get
            {
                int c = 0;
                if (!string.IsNullOrEmpty(Condition))
                {
                    c += Condition.Length;
                }
                if (!string.IsNullOrEmpty(StackTrace))
                {
                    c += StackTrace.Length;
                }
                return c;
            }
        }

        public LogEntry(string condition, string stackTrace)
        {
            Condition = condition;
            StackTrace = stackTrace;
        }
    }
}
