using System;
using System.Collections.Generic;
using System.Linq;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace MiyakoCarryService.Client.Utils
{
    /// <summary>  
    /// 运行时按单个 BotOwner 实时增删/激活/恢复 BigBrain 自定义 Layer，绕过 BrainManager 的全局 brainNames 匹配，直接操作目标 Bot 的大脑。 
    /// </summary>  
    public static class BrainUtils
    {
        private static int _currentLayerId = 15156;

        private static Type _customLayerWrapperType;
        private static bool _initialized = false;

        private static readonly Dictionary<string, Dictionary<string, int>> _injectedLayers = new();
        private static readonly Dictionary<string, Dictionary<string, (int Index, AICoreLayerClass<BotLogicDecision> Layer)>> _excludedLayers = new();

        private static void EnsureInit()
        {
            if (_initialized)
            {
                return;
            }

            _customLayerWrapperType = typeof(BrainManager).Assembly.GetType("DrakiaXYZ.BigBrain.Internal.CustomLayerWrapper");
            _initialized = true;
        }

        public static bool McsAddCustomLayer(BotOwner botOwner, Type customLayerType, int priority)
        {
            if (botOwner == null || botOwner.IsDead || botOwner.Brain?.BaseBrain == null)
            {
                return false;
            }

            EnsureInit();

            var layerName = customLayerType.Name;
            var map = _injectedLayers.TryGetValue(botOwner.ProfileId, out var m) ? m : (_injectedLayers[botOwner.ProfileId] = new());
            if (map.ContainsKey(layerName))
            {
                return false;
            }

            try
            {
                var wrapper = (AICoreLayerClass<BotLogicDecision>)Activator.CreateInstance(_customLayerWrapperType, [customLayerType, botOwner, priority]);

                var layerId = _currentLayerId++;
                if (!botOwner.Brain.BaseBrain.method_0(layerId, wrapper, true))
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
            var dict = baseBrain.Dictionary_0;

            foreach (var index in dict.Keys.ToList())
            {
                if (dict[index].Name() != layerName)
                {
                    continue;
                }

                var layer = dict[index];
                baseBrain.method_3(index);   // 从激活列表移除并 Deactivate  
                dict.Remove(index);          // method_3 不会删字典，需手动删  

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
            if (baseBrain.Dictionary_0.ContainsKey(cached.Index))
            {
                return false;
            }

            if (!baseBrain.method_0(cached.Index, cached.Layer, true))
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