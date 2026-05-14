using System;
using System.Collections.Generic;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Mgrs
{
    public static class EventMgr
    {
        private static Dictionary<Type, List<Delegate>> _eventHandlers = new();
        private static Dictionary<object, List<Type>> _subscriberEventTypes = new();

        public static void Subscribe<T>(Action<T> callback, object subscriber = null) where T : IMcsEvent
        {
            var eventType = typeof(T);

            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<Delegate>();
            }
            _eventHandlers[eventType].Add(callback);

            if (subscriber != null)
            {
                if (!_subscriberEventTypes.ContainsKey(subscriber))
                {
                    _subscriberEventTypes[subscriber] = new List<Type>();
                }
                _subscriberEventTypes[subscriber].Add(eventType);
            }
        }

        public static void Unsubscribe<T>(Action<T> callback) where T : IMcsEvent
        {
            var eventType = typeof(T);
            if (_eventHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(callback);
            }
        }

        public static void UnsubscribeAll(object subscriber)
        {
            if (_subscriberEventTypes.TryGetValue(subscriber, out var eventTypes))
            {
                foreach (var eventType in eventTypes)
                {
                    if (_eventHandlers.TryGetValue(eventType, out var handlers))
                    {
                        handlers.RemoveAll(h => h.Target == subscriber);
                    }
                }
                _subscriberEventTypes.Remove(subscriber);
            }
        }

        public static void Notify<T>(T @event) where T : IMcsEvent
        {
            var eventType = typeof(T);
            if (_eventHandlers.TryGetValue(eventType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        ((Action<T>)handler)(@event);
                    }
                    catch (Exception e)
                    {
                        MiyakoCarryServicePlugin.Logger.LogError($"事件处理错误 [{eventType.Name}]: {e}");
                    }
                }
            }
        }
    }
}