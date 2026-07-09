using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace MiyakoCarryService.Client.Utils
{
    /// <summary>  
    /// 运行时按单个 BotOwner 实时增删/激活/恢复 BigBrain 自定义 Layer，绕过 BrainManager 的全局 brainNames 匹配，直接操作目标 Bot 的大脑。 
    /// </summary>  
    internal static class LayerUtils
    {
        private static int _currentLayerId = 15156;

        private static Type _customLayerWrapperType;
        private static bool _initialized = false;

        private static readonly Dictionary<string, Dictionary<string, int>> _injectedLayers = new();
        private static readonly Dictionary<string, Dictionary<string, (int Index, AICoreLayer<BotLogicDecision> Layer)>> _excludedLayers = new();

        private static ConcurrentDictionary<Type, int> _customLayerMaps;

        private static void EnsureInit()
        {
            if (_initialized)
            {
                return;
            }

            _customLayerWrapperType = typeof(BrainManager).Assembly.GetType("DrakiaXYZ.BigBrain.Internal.CustomLayerWrapper");
            _initialized = true;
        }

        public static bool IsMcsBotPlayerInjected(string mcsBotPlayerId)
        {
            if (_injectedLayers.TryGetValue(mcsBotPlayerId, out var map))
            {
                var registerdLayerNames = _customLayerMaps.Keys.Select(l => l.Name);
                foreach ((var layerName, var layerId) in map)
                {
                    if (!registerdLayerNames.Contains(layerName))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static void RegisterCustomLayer(Type customLayerType, int priority)
        {
            if (_customLayerMaps == null)
            {
                _customLayerMaps = new();
            }

            _customLayerMaps.AddOrUpdate(customLayerType, priority, 
                (customLayerType, oldPriority) =>
                {
                    oldPriority = priority;
                    return oldPriority;
                }
            );
        }

        public static void OnRaidEnded()
        {
            _injectedLayers.Clear();
            _excludedLayers.Clear();
        }

        public static ConcurrentDictionary<Type, int> GetCustomLayerMaps()
        {
            return _customLayerMaps;
        }

        public static bool McsAddCustomLayer(BotOwner botOwner, Type customLayerType, int priority)
        {
            if (botOwner == null || botOwner.IsDead || botOwner.Brain?.BaseBrain == null)
            {
                return false;
            }

            EnsureInit();

            var layerName = customLayerType.Name;
            var map = _injectedLayers.TryGetValue(botOwner.ProfileId, out var _map) ? _map : (_injectedLayers[botOwner.ProfileId] = new());
            if (map.ContainsKey(layerName))
            {
                return false;
            }

            try
            {
                var wrapper = (AICoreLayer<BotLogicDecision>)Activator.CreateInstance(_customLayerWrapperType, [customLayerType, botOwner, priority]);

                var layerId = _currentLayerId++;
                if (!botOwner.Brain.BaseBrain.TryAddLayer(layerId, wrapper, true))
                {
                    return false;
                }

                map[layerName] = layerId;
                return true;
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return false;
            }
        }

        public static bool McsRemoveLayer(BotOwner botOwner, string layerName)
        {
            if (botOwner == null || botOwner.Brain?.BaseBrain == null)
            {
                return false;
            }

            var baseBrain = botOwner.Brain.BaseBrain;
            var dict = baseBrain.dictionary_0;

            foreach (var index in dict.Keys.ToList())
            {
                if (dict[index].Name() != layerName)
                {
                    continue;
                }

                var layer = dict[index];
                baseBrain.DeactivateLayer(index);
                dict.Remove(index);

                var excluded = _excludedLayers.TryGetValue(botOwner.ProfileId, out var em) ? em : (_excludedLayers[botOwner.ProfileId] = new());
                excluded[layerName] = (index, layer);

                if (_injectedLayers.TryGetValue(botOwner.ProfileId, out var im))
                {
                    im.Remove(layerName);
                }
                return true;
            }
            return false;
        }

        public static void McsRemoveLayers(BotOwner botOwner, IEnumerable<string> layerNames)
        {
            if (botOwner == null || botOwner.Brain?.BaseBrain == null || layerNames == null)
            {
                return;
            }

            foreach (var layerName in layerNames)
            {
                McsRemoveLayer(botOwner, layerName);
            }
        }

        public static bool McsRestoreLayer(BotOwner botOwner, string layerName)
        {
            if (botOwner == null || botOwner.Brain?.BaseBrain == null)
            {
                return false;
            }

            if (!_excludedLayers.TryGetValue(botOwner.ProfileId, out var excluded) || !excluded.TryGetValue(layerName, out var cached))
            {
                return false;
            }

            var baseBrain = botOwner.Brain.BaseBrain;
            if (baseBrain.dictionary_0.ContainsKey(cached.Index))
            {
                return false;
            }

            if (!baseBrain.TryAddLayer(cached.Index, cached.Layer, true))
            {
                return false;
            }

            excluded.Remove(layerName);
            var map = _injectedLayers.TryGetValue(botOwner.ProfileId, out var m) ? m : (_injectedLayers[botOwner.ProfileId] = new());
            map[layerName] = cached.Index;
            return true;
        }

        public static void McsRestoreLayers(BotOwner botOwner, IEnumerable<string> layerNames)
        {
            if (botOwner == null || botOwner.Brain?.BaseBrain == null || layerNames == null)
            {
                return;
            }

            foreach (var layerName in layerNames)
            {
                McsRestoreLayer(botOwner, layerName);
            }
        }

        public static bool McsHasLayer(BotOwner botOwner, string layerName)
        {
            return botOwner != null && _injectedLayers.TryGetValue(botOwner.ProfileId, out var m) && m.ContainsKey(layerName);
        }
    }
}