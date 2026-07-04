
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
        /// <param name="layerType"></param>
        /// <param name="priority"></param>
        public static void RegisterCustomLayer(Type layerType, int priority)
        {
            LayerUtils.RegisterCustomLayer(layerType, priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="botOwner"></param>
        /// <param name="layerType"></param>
        /// <param name="priority"></param>
        public static void McsAddCustomLayer(BotOwner botOwner, Type layerType, int priority)
        {
            LayerUtils.McsAddCustomLayer(botOwner, layerType, priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="botOwner"></param>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public static bool McsHasLayer(BotOwner botOwner, string layerName)
        {
            return LayerUtils.McsHasLayer(botOwner, layerName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="botOwner"></param>
        /// <param name="layerName"></param>
        public static void McsRemoveLayer(BotOwner botOwner, string layerName)
        {
            LayerUtils.McsRemoveLayer(botOwner, layerName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="botOwner"></param>
        /// <param name="layerNames"></param>
        public static void McsRemoveLayers(BotOwner botOwner, IEnumerable<string> layerNames)
        {
            LayerUtils.McsRemoveLayers(botOwner, layerNames);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="botOwner"></param>
        /// <param name="layerName"></param>
        public static void McsRestoreLayer(BotOwner botOwner, string layerName)
        {
            LayerUtils.McsRestoreLayer(botOwner, layerName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="botOwner"></param>
        /// <param name="layerNames"></param>
        public static void McsRestoreLayers(BotOwner botOwner, IEnumerable<string> layerNames)
        {
            LayerUtils.McsRestoreLayers(botOwner, layerNames);
        }
    }
}