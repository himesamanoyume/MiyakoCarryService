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
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        /// <param name="subscriber"></param>
        public static void Subscribe<T>(Action<T> callback, object subscriber = null) where T : IMcsEvent
        {
            EventMgr.Subscribe(callback, subscriber);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public static void Unsubscribe<T>(Action<T> callback) where T : IMcsEvent
        {
            EventMgr.Subscribe(callback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscriber"></param>
        public static void UnsubscribeAll(object subscriber)
        {
            EventMgr.UnsubscribeAll(subscriber);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        public static void Notify<T>(T @event) where T : IMcsEvent
        {
            EventMgr.Notify(@event);
        }
    }
}