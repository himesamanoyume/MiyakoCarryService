using System;
using System.Collections.Generic;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Misc;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    public static class ItemDataUpdateDebouncer
    {
        private static readonly Dictionary<string, float> _lastUpdateTimes = new Dictionary<string, float>();
        private static readonly float _debounceInterval = 1f; 

        /// <summary>  
        /// 尝试执行防抖更新  
        /// </summary>  
        /// <param name="item">物品</param>  
        /// <param name="mcsAILeadPlayer">AI玩家</param>  
        /// <param name="action">要执行的操作</param>  
        public static void TryDebouncedUpdate(Item item, McsAILeadPlayer mcsAILeadPlayer, Action action)
        {
            if (item == null || action == null)
            {
                return;
            }

            var key = $"{item.Id}_{mcsAILeadPlayer.Player().ProfileId}";
            var currentTime = Time.time;

            if (_lastUpdateTimes.TryGetValue(key, out float lastUpdateTime))
            {
                if (currentTime - lastUpdateTime < _debounceInterval)
                {
                    return;
                }
            }

            _lastUpdateTimes[key] = currentTime;
            action();
        }

        /// <summary>  
        /// 清理指定物品的防抖记录  
        /// </summary>  
        public static void ClearItemRecord(Item item)
        {
            if (item == null)
            {
                return;
            }

            var prefix = $"{item.Id}_";
            var keysToRemove = new List<string>();

            foreach (var key in _lastUpdateTimes.Keys)
            {
                if (key.StartsWith(prefix))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _lastUpdateTimes.Remove(key);
            }
        }

        /// <summary>  
        /// 清理所有防抖记录（战局结束时调用）  
        /// </summary>  
        public static void ClearAllRecords()
        {
            _lastUpdateTimes.Clear();
        }
    }
}