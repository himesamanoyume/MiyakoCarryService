using System;
using System.Threading;
using Comfort.Common;
using EFT;
using EFT.Communications;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Events;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Services;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Assistant.Mgrs
{
    /// <summary>
    /// Assistant 语音管线编排单例。在 Plugin GameObject 上挂为组件，由 Unity 自动调用 Awake/Update/OnDestroy。
    /// 状态机：Idle → Capturing → Transcribing → Interpreting → Dispatching → Idle。
    /// <para>
    /// PushToTalk: 按住 <see cref="AssistantVoiceConfig.HotKey"/> 录音，松开后端到端处理一次；
    /// FreeTalk: 持续采样 + RMS VAD 自动起止，无按键。
    /// </para>
    /// </summary>
    public sealed class VoiceMgr : MonoBehaviour
    {
        public static VoiceMgr Instance;

        private AssistantVoiceConfig _cfg;
        private AudioCaptureService _capture;
        private VadService _vad;
        private SttDispatcher _stt;
        private LlmDispatcher _llm;

        private EVoiceState _state = EVoiceState.Idle;
        private bool _capturing;
        private float _captureStartedAt;
        private CancellationTokenSource _processingCts;

        // FreeTalk: VAD 状态
        private float _lastSpeechAt;
        private bool _speechStarted;
        private float _windowPeriodSeconds = 0.05f;
        private float _nextWindowAt;

        // 跨帧派发：异步任务完成后写入，Update 主线程消费。
        private LlmIntent _pendingIntent;
        private string _pendingTranscribedText;

        void Awake()
        {
            Instance = this;
            _cfg = MiyakoCarryServiceAssistantPlugin.Instance.VoiceConfig;
            _capture = new AudioCaptureService();
            _vad = new VadService(new VadParams
            {
                EnergyThreshold = _cfg.VadEnergyThreshold,
                SilenceSeconds = _cfg.VadSilenceSeconds,
            });
            _stt = new SttDispatcher(_cfg.SttProvider);
            _llm = new LlmDispatcher(_cfg.LlmProvider);
        }

        void OnDestroy()
        {
            try { _processingCts?.Cancel(); } catch { }
            try { if (_capture.IsCapturing) _capture.End(); } catch { }
            Instance = null;
        }

        public void Destroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            // 配置项可被玩家在 ConfigurationManager 中实时修改；每帧轻量刷新敏感字段
            if (_cfg == null)
            {
                return;
            }

            var liveCfg = MiyakoCarryServiceAssistantPlugin.Instance.VoiceConfig;
            if (liveCfg != null)
            {
                _vad = new VadService(new VadParams
                {
                    EnergyThreshold = liveCfg.VadEnergyThreshold,
                    SilenceSeconds = liveCfg.VadSilenceSeconds,
                });
            }

            // 主线程消费异步结果
            if (_state == EVoiceState.Dispatching && _pendingIntent != null)
            {
                ConsumePendingIntent();
            }

            if (!_cfg.Enabled || !TargetResolver.IsInRaid())
            {
                if (_capturing) { EndCapture(); }
                _state = EVoiceState.Idle;
                return;
            }

            switch (_cfg.TriggerMode)
            {
                case EVoiceTriggerMode.PushToTalk: HandlePushToTalk(); break;
                case EVoiceTriggerMode.FreeTalk: HandleFreeTalk(); break;
            }
        }

        private void HandlePushToTalk()
        {
            var hotKey = _cfg.HotKey;
            bool down = MiyakoCarryService.Client.Utils.KeyInput.BetterIsDown(hotKey);

            if (down && !_capturing && _state == EVoiceState.Idle)
            {
                BeginCapture();
            }
            else if (!down && _capturing && _state == EVoiceState.Capturing)
            {
                EndCapture();
            }
            else if (_capturing && _state == EVoiceState.Capturing && Time.unscaledTime - _captureStartedAt > _cfg.CaptureMaxSeconds)
            {
                EndCapture();
            }
        }

        private void HandleFreeTalk()
        {
            if (_state == EVoiceState.Idle && !_capturing)
            {
                BeginCapture();
                return;
            }

            if (!_capturing) { return; }

            // 周期性采样窗口检测 RMS
            if (Time.unscaledTime < _nextWindowAt) { return; }
            _nextWindowAt = Time.unscaledTime + _windowPeriodSeconds;

            var clip = _capture.ActiveClip;
            if (clip == null) { return; }
            int currentPos = Microphone.GetPosition(null);
            if (currentPos <= 0) { return; }
            int windowSize = (int)(_capture.SampleRate * _windowPeriodSeconds);
            if (currentPos < windowSize) { return; }

            var window = new float[windowSize];
            clip.GetData(window, currentPos - windowSize);

            float rms = _vad.ComputeRms(window);
            if (_vad.IsSpeech(rms))
            {
                _lastSpeechAt = Time.unscaledTime;
                if (!_speechStarted)
                {
                    _speechStarted = true;
                }
            }
            else if (_speechStarted && _vad.ShouldStopAfterSilence(rms, Time.unscaledTime - _lastSpeechAt))
            {
                // 静默超时结束 → 进入处理
                _speechStarted = false;
                EndCapture();
            }
            else if (_captureStartedAt > 0 && Time.unscaledTime - _captureStartedAt > _cfg.CaptureMaxSeconds)
            {
                _speechStarted = false;
                EndCapture();
            }
        }

        private void BeginCapture()
        {
            if (_capturing) { return; }
            if (!_capture.Begin())
            {
                Notification("麦克风不可用或被占用");
                return;
            }
            _capturing = true;
            _captureStartedAt = Time.unscaledTime;
            _speechStarted = false;
            _lastSpeechAt = Time.unscaledTime;
            _state = EVoiceState.Capturing;
        }

        private void EndCapture()
        {
            if (!_capturing) { return; }
            var samples = _capture.End();
            _capturing = false;

            if (samples == null || samples.Length == 0)
            {
                _state = EVoiceState.Idle;
                return;
            }

            try { _processingCts?.Cancel(); } catch { }
            _processingCts = new CancellationTokenSource();
            _ = ProcessCaptureAsync(samples, _processingCts.Token);
        }

        private async System.Threading.Tasks.Task ProcessCaptureAsync(float[] samples, CancellationToken ct)
        {
            _state = EVoiceState.Transcribing;
            var segment = new AudioSegment
            {
                Samples = samples,
                SampleRate = _capture.SampleRate,
                Channels = _capture.Channels,
            };

            SttResult stt;
            try
            {
                stt = await _stt.TranscribeAsync(segment, _cfg.SttSettings, ct).ConfigureAwait(true);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                MiyakoCarryServiceAssistantPlugin.Logger.LogError($"STT 异常：{ex}");
                _pendingTranscribedText = string.Empty;
                _pendingIntent = new LlmIntent { Error = $"STT 异常：{ex.Message}" };
                _state = EVoiceState.Dispatching;
                return;
            }

            if (!stt.IsSuccess || string.IsNullOrWhiteSpace(stt.Text))
            {
                _pendingTranscribedText = stt?.Text ?? string.Empty;
                _pendingIntent = new LlmIntent { Error = stt?.Error ?? Utils.Locales.VOICESTTFAILED };
                _state = EVoiceState.Dispatching;
                return;
            }

            _state = EVoiceState.Interpreting;
            _pendingTranscribedText = stt.Text;

            LlmIntent intent;
            try
            {
                intent = await _llm.InterpretAsync(stt.Text, _cfg.LlmSettings, ct).ConfigureAwait(true);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                MiyakoCarryServiceAssistantPlugin.Logger.LogError($"LLM 异常：{ex}");
                _pendingIntent = new LlmIntent { Error = $"LLM 异常：{ex.Message}" };
                _state = EVoiceState.Dispatching;
                return;
            }

            _pendingIntent = intent;
            _state = EVoiceState.Dispatching;
        }

        private void ConsumePendingIntent()
        {
            var intent = _pendingIntent;
            var transcribed = _pendingTranscribedText;
            _pendingIntent = null;
            _pendingTranscribedText = null;
            _state = EVoiceState.Idle;

            if (intent == null) { return; }

            int dispatched = 0;
            string feedback = null;

            if (intent.IsError)
            {
                feedback = intent.Error;
            }
            else if (intent.IsReply)
            {
                feedback = intent.ReplyText;
            }
            else if (!string.IsNullOrEmpty(intent.CommandName))
            {
                try
                {
                    dispatched = IntentBinder.BindAndDispatch(intent);
                    feedback = dispatched > 0
                        ? $"已下发 {dispatched} 名护航：{intent.CommandName}"
                        : $"无匹配护航成员：{intent.CommandName}";
                }
                catch (Exception ex)
                {
                    MiyakoCarryServiceAssistantPlugin.Logger.LogError($"BindAndDispatch 异常：{ex}");
                    feedback = $"派发异常：{ex.Message}";
                }
            }
            else
            {
                feedback = "未识别到指令";
            }

            McsEventApi.Notify(new VoiceCommandEvent
            {
                TranscribedText = transcribed,
                Intent = intent,
                State = _state,
                DispatchedMembers = dispatched,
                FeedbackMessage = feedback,
            });

            if (_cfg.FeedbackSubtitles && !string.IsNullOrEmpty(feedback))
            {
                Notification(feedback);
            }
        }

        private static void Notification(string message)
        {
            try
            {
                if (Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance != null)
                {
                    NotificationManager.DisplayMessageNotification(message);
                }
            }
            catch
            {
                // 忽略——通知失败不应阻断语音管线
            }
        }
    }
}