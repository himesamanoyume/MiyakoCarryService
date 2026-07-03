
using System;
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
            BrainUtils.RegisterCustomLayer(layerType, priority);
        }
    }
}