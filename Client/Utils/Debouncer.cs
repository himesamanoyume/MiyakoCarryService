using System;
using System.Collections;
using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Misc;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    public class Debouncer<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _pendingUpdates = new();
        private readonly float _delaySeconds;
        private readonly Action<Dictionary<TKey, TValue>> _batchCallback;
        private readonly MonoBehaviour _coroutineRunner;
        private readonly Func<TValue, TValue, TValue> _mergeFunc;
        private Coroutine _debounceCoroutine;
        private bool _isRunning;

        public Debouncer(MonoBehaviour coroutineRunner, float delaySeconds, Action<Dictionary<TKey, TValue>> batchCallback) : this(coroutineRunner, delaySeconds, batchCallback, null)
        {
            
        }

        public Debouncer(MonoBehaviour coroutineRunner, float delaySeconds, Action<Dictionary<TKey, TValue>> batchCallback, Func<TValue, TValue, TValue> mergeFunc)
        {
            _coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
            _delaySeconds = delaySeconds;
            _batchCallback = batchCallback ?? throw new ArgumentNullException(nameof(batchCallback));
            _mergeFunc = mergeFunc;
        }

        public void Trigger(TKey key, TValue value)
        {
            if (_pendingUpdates.TryGetValue(key, out var existingValue))
            {
                _pendingUpdates[key] = _mergeFunc != null ? _mergeFunc(existingValue, value) : value;
            }
            else
            {
                _pendingUpdates[key] = value;
            }

            if (!_isRunning)
            {
                _isRunning = true;
                _debounceCoroutine = _coroutineRunner.StartCoroutine(DebounceCoroutine());
            }
            else
            {
                if (_debounceCoroutine != null)
                {
                    _coroutineRunner.StopCoroutine(_debounceCoroutine);
                }
                _debounceCoroutine = _coroutineRunner.StartCoroutine(DebounceCoroutine());
            }
        }

        private IEnumerator DebounceCoroutine()
        {
            yield return new WaitForSeconds(_delaySeconds);

            if (_pendingUpdates.Count > 0)
            {
                var snapshot = new Dictionary<TKey, TValue>(_pendingUpdates);
                _pendingUpdates.Clear();

                try
                {
                    _batchCallback(snapshot);
                }
                catch (Exception e)
                {
                    MiyakoCarryServicePlugin.Logger.LogError($"Debouncer batch callback error: {e}");
                }
            }

            _isRunning = false;
            _debounceCoroutine = null;
        }

        public void Flush()
        {
            if (_debounceCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_debounceCoroutine);
                _debounceCoroutine = null;
            }

            if (_pendingUpdates.Count > 0)
            {
                var snapshot = new Dictionary<TKey, TValue>(_pendingUpdates);
                _pendingUpdates.Clear();

                try
                {
                    _batchCallback(snapshot);
                }
                catch (Exception e)
                {
                    MiyakoCarryServicePlugin.Logger.LogError($"Debouncer flush error: {e}");
                }
            }

            _isRunning = false;
        }

        public void Clear()
        {
            _pendingUpdates.Clear();
            if (_debounceCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_debounceCoroutine);
                _debounceCoroutine = null;
            }
            _isRunning = false;
        }

        public static implicit operator Debouncer<TKey, TValue>(Debouncer<ItemData, McsAILeadPlayer> v)
        {
            throw new NotImplementedException();
        }
    }
}