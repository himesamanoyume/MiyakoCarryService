
using System;
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Api
{
    public static class McsLayerApi
    {
        /// <summary>
        /// 
        /// </summary>
        public static void RegisterCustomLayer(Type layerType, int priority)
        {
            LayerUtils.RegisterCustomLayer(layerType, priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void McsAddCustomLayer(BotOwner botOwner, Type layerType, int priority)
        {
            LayerUtils.McsAddCustomLayer(botOwner, layerType, priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool McsHasLayer(BotOwner botOwner, string layerName)
        {
            return LayerUtils.McsHasLayer(botOwner, layerName);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void McsRemoveLayer(BotOwner botOwner, string layerName)
        {
            LayerUtils.McsRemoveLayer(botOwner, layerName);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void McsRemoveLayers(BotOwner botOwner, IEnumerable<string> layerNames)
        {
            LayerUtils.McsRemoveLayers(botOwner, layerNames);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void McsRestoreLayer(BotOwner botOwner, string layerName)
        {
            LayerUtils.McsRestoreLayer(botOwner, layerName);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void McsRestoreLayers(BotOwner botOwner, IEnumerable<string> layerNames)
        {
            LayerUtils.McsRestoreLayers(botOwner, layerNames);
        }
    }
}