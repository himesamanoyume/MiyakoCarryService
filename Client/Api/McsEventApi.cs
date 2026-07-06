using System;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Mgrs;

namespace MiyakoCarryService.Client.Api
{
    public static class McsEventApi
    {
        /// <summary>
        /// 
        /// </summary>
        public static void Subscribe<T>(Action<T> callback, object subscriber = null) where T : IMcsEvent
        {
            EventMgr.Subscribe(callback, subscriber);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Unsubscribe<T>(Action<T> callback) where T : IMcsEvent
        {
            EventMgr.Subscribe(callback);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void UnsubscribeAll(object subscriber)
        {
            EventMgr.UnsubscribeAll(subscriber);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Notify<T>(T @event) where T : IMcsEvent
        {
            EventMgr.Notify(@event);
        }
    }
}