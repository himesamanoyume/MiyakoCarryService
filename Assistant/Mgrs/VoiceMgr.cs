using System;
using System.Threading;
using Comfort.Common;
using EFT;
using EFT.Communications;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Events;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Services;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Assistant.Mgrs
{
    /// <summary>
    /// Assistant 语音管线编排器
    /// </summary>
    public sealed class VoiceMgr : BaseMgr
    {
        private AudioCaptureService _capture;
        private VadService _vad;
        private SttDispatcher _stt;
        private LlmDispatcher _llm;
        private EVoiceState _state = EVoiceState.Idle;
        private bool _capturing;
        private float _captureStartedAt;
        private CancellationTokenSource _processingCts;
        private float _lastSpeechAt;
        private bool _speechStarted;
        private int _speechConfirmCount;
        private float _windowPeriodSeconds = 0.05f;
        private float _nextWindowAt;
        private LlmIntent _pendingIntent;
        private string _pendingTranscribedText;

        void Awake()
        {
            _capture = new AudioCaptureService();
            _vad = new VadService(new VadParams
            {
                EnergyThreshold = MiyakoCarryServiceAssistantPlugin.VoiceVadEnergyThreshold.Value,
                SilenceSeconds = MiyakoCarryServiceAssistantPlugin.VoiceVadSilenceSeconds.Value,
            });
            _stt = new SttDispatcher(MiyakoCarryServiceAssistantPlugin.SttProvider.Value);
            _llm = new LlmDispatcher(MiyakoCarryServiceAssistantPlugin.LlmProvider.Value);
        }

        void OnDestroy()
        {
            try { _processingCts?.Cancel(); } catch { }
            try { _capture.Stop(); } catch { }
            base.OnMgrDestroy();
        }

        void Update()
        {
            // 配置项可被玩家在 ConfigurationManager 中实时修改；仅当参数变化时重建 VAD，
            // 避免每帧重建导致自适应噪音地板状态丢失
            var energyThreshold = MiyakoCarryServiceAssistantPlugin.VoiceVadEnergyThreshold.Value;
            var silenceSeconds = MiyakoCarryServiceAssistantPlugin.VoiceVadSilenceSeconds.Value;
            if (Math.Abs(_vad.EnergyThreshold - energyThreshold) > 0.0001f ||
                Math.Abs(_vad.SilenceSeconds - silenceSeconds) > 0.0001f)
            {
                _vad = new VadService(new VadParams
                {
                    EnergyThreshold = energyThreshold,
                    SilenceSeconds = silenceSeconds,
                });
            }

            // 主线程消费异步结果
            if (_state == EVoiceState.Dispatching && _pendingIntent != null)
            {
                ConsumePendingIntent();
            }

            // 正常语音管线：需要 VoiceEnabled 且处于战局
            if (!MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value && (!MiyakoCarryServiceAssistantPlugin.VoiceEnabled.Value || !GameLoop.Instance.IsVaildGameWorld))
            {
                if (_capturing)
                {
                    // 门控关闭：丢弃当前段（麦克风会话保持，避免 End→Start 循环失败）
                    _capture.Abort();
                }
                _state = EVoiceState.Idle;
                return;
            }

            // 录音期间每帧把新样本从循环麦克风缓冲累积到内部缓冲（不录音时为无操作）。
            // FreeTalk：语音确认（_speechStarted）前不累积，待机空白从源头不进缓冲；
            // PushToTalk：按键期间持续累积
            if (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value == EVoiceTriggerMode.PushToTalk || _speechStarted)
            {
                _capture.Poll();
            }

            // STT 调试模式：战局内外均可录音（菜单/藏身处也能测试麦克风与转写），
            // 不再受 inRaid 限制，继续按当前触发模式流程执行
            switch (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value)
            {
                case EVoiceTriggerMode.PushToTalk:
                    HandlePushToTalk();
                    break;
                case EVoiceTriggerMode.FreeTalk:
                    HandleFreeTalk();
                    break;
            }
        }

        private void HandlePushToTalk()
        {
            var isPressed = KeyInput.BetterIsPressed(MiyakoCarryServiceAssistantPlugin.VoiceHotKey.Value);

            if (isPressed && !_capturing && _state == EVoiceState.Idle)
            {
                BeginCapture();
            }
            else if (!isPressed && _capturing && _state == EVoiceState.Capturing)
            {
                EndCapture();
            }
            else if (_capturing && _state == EVoiceState.Capturing && Time.unscaledTime - _captureStartedAt > MiyakoCarryServiceAssistantPlugin.VoiceCaptureMaxSeconds.Value)
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

            if (!_capturing)
            {
                return;
            }

            // 周期性采样窗口检测 RMS
            if (Time.unscaledTime < _nextWindowAt)
            {
                return;
            }
            _nextWindowAt = Time.unscaledTime + _windowPeriodSeconds;

            var clip = _capture.ActiveClip;
            if (clip == null)
            {
                return;
            }
            // 使用与录音一致的设备位置（AudioCaptureService 内部缓存设备名），
            // 避免非默认设备时 VAD 窗口位置查询指向未录音的设备
            int currentPos = _capture.CurrentPosition;
            if (currentPos <= 0)
            {
                return;
            }
            int windowSize = (int)(_capture.SampleRate * _windowPeriodSeconds);
            if (currentPos < windowSize)
            {
                return;
            }
            if (currentPos > clip.samples)
            {
                return;
            }

            var window = new float[windowSize];
            clip.GetData(window, currentPos - windowSize);

            float rms = _vad.ComputeRms(window);
            // 用本窗 RMS 更新自适应噪音地板（语音窗自动排除），再判定语音
            _vad.UpdateNoiseFloor(rms);

            // STT 调试模式：逐窗输出 VAD 现场值，便于实测底噪并精调阈值
            if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
            {
                MiyakoCarryServiceAssistantPlugin.Logger.LogInfo(
                    $"VAD rms={rms:F4} speech={_vad.IsSpeech(rms)} silence={Time.unscaledTime - _lastSpeechAt:F2}s");
            }

            if (_vad.IsSpeech(rms))
            {
                _lastSpeechAt = Time.unscaledTime;
                _speechConfirmCount++;
                // 连续 2 窗（100ms）确认语音后才置位并丢弃之前的累积（含待机空白与噪音误触发），
                // 让录音从真正的语音开始
                if (!_speechStarted && _speechConfirmCount >= 2)
                {
                    _speechStarted = true;
                    _capture.Reset();
                }
            }
            else
            {
                _speechConfirmCount = 0;
                if (_speechStarted && _vad.ShouldStopAfterSilence(rms, Time.unscaledTime - _lastSpeechAt))
                {
                    _speechStarted = false;
                    EndCapture();
                }
                else if (_captureStartedAt > 0 && Time.unscaledTime - _captureStartedAt > MiyakoCarryServiceAssistantPlugin.VoiceCaptureMaxSeconds.Value)
                {
                    _speechStarted = false;
                    EndCapture();
                }
            }
        }

        private void BeginCapture()
        {
            if (_capturing)
            {
                return;
            }
            // STT 调试模式：开始录音时先显示录音中状态，转写结果返回后再覆盖
            if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
            {
                MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = "正在录音";
            }
            if (!_capture.Begin())
            {
                Notification("麦克风不可用或被占用");
                return;
            }
            _capturing = true;
            _captureStartedAt = Time.unscaledTime;
            _speechStarted = false;
            _speechConfirmCount = 0;
            _lastSpeechAt = Time.unscaledTime;
            _state = EVoiceState.Capturing;
        }

        private void EndCapture()
        {
            if (!_capturing)
            {
                return;
            }
            var samples = _capture.End();
            _capturing = false;

            // 自由说话：去掉结束后的静音尾巴，只保留说话内容
            if (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value == EVoiceTriggerMode.FreeTalk)
            {
                samples = TrimTrailingSilence(samples);
            }

            MiyakoCarryServiceAssistantPlugin.Logger.LogInfo(
                $"录音结束：{(samples == null ? 0 : samples.Length) / (float)_capture.SampleRate:F2}s，{(samples == null ? 0 : samples.Length)} 样本");

            if (samples == null || samples.Length == 0)
            {
                // STT 调试模式：未捕获到音频时给出提示，避免"正在录音"状态卡死
                if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
                {
                    MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = "未捕获到音频";
                }
                _state = EVoiceState.Idle;
                return;
            }

            try
            {
                _processingCts?.Cancel();
            }
            catch
            {

            }
            _processingCts = new CancellationTokenSource();
            _ = ProcessCaptureAsync(samples, _processingCts.Token);
        }

        /// <summary>裁剪 FreeTalk 录音尾部的静音（按 VAD 能量阈值以窗口为单位从末尾回退）。</summary>
        private float[] TrimTrailingSilence(float[] samples)
        {
            if (samples == null || samples.Length == 0)
            {
                return samples;
            }
            int window = (int)(_capture.SampleRate * _windowPeriodSeconds);
            if (window <= 0 || samples.Length < window)
            {
                return samples;
            }

            int end = samples.Length;
            while (end >= window)
            {
                var win = new float[window];
                Array.Copy(samples, end - window, win, 0, window);
                if (_vad.IsSpeech(_vad.ComputeRms(win)))
                {
                    break;
                }
                end -= window;
            }

            if (end <= 0)
            {
                return Array.Empty<float>();
            }
            if (end >= samples.Length)
            {
                return samples;
            }
            var trimmed = new float[end];
            Array.Copy(samples, trimmed, end);
            return trimmed;
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

            var sttSettings = new ProviderSettings
            {
                ApiKey = MiyakoCarryServiceAssistantPlugin.SttApiKey.Value,
                BaseUrl = MiyakoCarryServiceAssistantPlugin.SttBaseUrl.Value,
                ModelId = MiyakoCarryServiceAssistantPlugin.SttModelId.Value,
                Language = MiyakoCarryServiceAssistantPlugin.SttLanguage.Value,
                TimeoutSec = MiyakoCarryServiceAssistantPlugin.SttTimeoutSec.Value,
            };

            var llmSettings = new ProviderSettings
            {
                ApiKey = MiyakoCarryServiceAssistantPlugin.LlmApiKey.Value,
                BaseUrl = MiyakoCarryServiceAssistantPlugin.LlmBaseUrl.Value,
                ModelId = MiyakoCarryServiceAssistantPlugin.LlmModelId.Value,
                SystemPrompt = MiyakoCarryServiceAssistantPlugin.LlmSystemPrompt.Value,
                Temperature = MiyakoCarryServiceAssistantPlugin.LlmTemperature.Value,
                MaxTokens = MiyakoCarryServiceAssistantPlugin.LlmMaxTokens.Value,
                TimeoutSec = MiyakoCarryServiceAssistantPlugin.LlmTimeoutSec.Value,
            };

            SttResult stt;
            try
            {
                stt = await _stt.TranscribeAsync(segment, sttSettings, ct).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                MiyakoCarryServiceAssistantPlugin.Logger.LogError($"STT 异常：{ex}");
                if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
                {
                    MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = $"STT 异常：{ex.Message}";
                    _state = EVoiceState.Idle;
                    return;
                }
                _pendingTranscribedText = string.Empty;
                _pendingIntent = new LlmIntent { Error = $"STT 异常：{ex.Message}" };
                _state = EVoiceState.Dispatching;
                return;
            }

            if (!stt.IsSuccess || string.IsNullOrWhiteSpace(stt.Text))
            {
                if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
                {
                    MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = stt?.Error ?? Utils.Locales.VOICESTTFAILED.McsLocalized();
                    _state = EVoiceState.Idle;
                    return;
                }
                _pendingTranscribedText = stt?.Text ?? string.Empty;
                _pendingIntent = new LlmIntent { Error = stt?.Error ?? Utils.Locales.VOICESTTFAILED.McsLocalized() };
                _state = EVoiceState.Dispatching;
                return;
            }

            // STT 调试模式：转写文本覆盖写入调试字段，跳过 LLM 解释与派发
            if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
            {
                MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = stt.Text;
                _state = EVoiceState.Idle;
                return;
            }

            _state = EVoiceState.Interpreting;
            _pendingTranscribedText = stt.Text;

            LlmIntent intent;
            try
            {
                intent = await _llm.InterpretAsync(stt.Text, llmSettings, ct).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                return;
            }
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

            if (intent == null)
            {
                return;
            }

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

            if (MiyakoCarryServiceAssistantPlugin.VoiceFeedbackSubtitles.Value && !string.IsNullOrEmpty(feedback))
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

            }
        }
    }
}