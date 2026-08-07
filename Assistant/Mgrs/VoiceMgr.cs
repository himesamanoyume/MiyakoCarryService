using System;
using System.Text;
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
using MiyakoCarryService.Client.Models;
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
        // 记录当前生效的服务商，配置变化时重建分发器（修复游戏内切换服务商不生效的问题）
        private ESttProvider _sttProvider;
        private ELlmProvider _llmProvider;
        private EVoiceState _state = EVoiceState.Idle;
        private bool _capturing;
        private float _captureStartedAt;
        private CancellationTokenSource _processingCts;
        private float _lastSpeechAt;
        private bool _speechStarted;
        private int _speechConfirmCount;
        // FreeTalk 最短有效语音时长兜底：拦截异常残留的极短/空音频，避免触发 STT
        private const float MinFreeTalkSpeechSeconds = 0.3f;
        // STT 调试模式：FreeTalk 上次返回结果（跨段持久），用于"正在监听：结果"显示
        private string _debugLastResult;
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
            _sttProvider = MiyakoCarryServiceAssistantPlugin.SttProvider.Value;
            _llmProvider = MiyakoCarryServiceAssistantPlugin.LlmProvider.Value;
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

            // 服务商可被玩家在 ConfigurationManager 中实时修改；变化时重建分发器
            var sttProvider = MiyakoCarryServiceAssistantPlugin.SttProvider.Value;
            if (_sttProvider != sttProvider)
            {
                _stt = new SttDispatcher(sttProvider);
                _sttProvider = sttProvider;
            }
            var llmProvider = MiyakoCarryServiceAssistantPlugin.LlmProvider.Value;
            if (_llmProvider != llmProvider)
            {
                _llm = new LlmDispatcher(llmProvider);
                _llmProvider = llmProvider;
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

            // 每帧轮询麦克风：样本始终喂入滚动预卷；是否累积到段缓冲由 AudioCaptureService
            // 内部 _armed 决定（PTT 开始即置位；FreeTalk 语音确认后置位），不录音时为无操作
            _capture.Poll();

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

            // STT 调试模式：逐窗输出 VAD 现场值到调试只读文本（实时刷新，便于实测底噪并精调阈值）
            if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
            {
                MiyakoCarryServiceAssistantPlugin.VoiceDebugVadText.Value =
                    $"rms={rms:F4} speech={_vad.IsSpeech(rms)} silence={Time.unscaledTime - _lastSpeechAt:F2}s";
            }

            if (_vad.IsSpeech(rms))
            {
                _lastSpeechAt = Time.unscaledTime;
                _speechConfirmCount++;
                // 连续 2 窗（100ms）确认语音后才置位并武装累积：段起点取内部预卷（含语音起音），
                // 避免截断第一个字，同时丢弃确认前的待机空白与噪音误触发
                if (!_speechStarted && _speechConfirmCount >= 2)
                {
                    _speechStarted = true;
                    _capture.Arm();
                    // STT 调试模式：触发阈值确认语音，正式进入录音
                    if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
                    {
                        MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = "正在录音";
                    }
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
                    if (_speechStarted)
                    {
                        // 说话中/刚说完超时：正常结束并发送
                        _speechStarted = false;
                        EndCapture();
                    }
                    else
                    {
                        // 全程无语音：丢弃累积、重同步游标并重置段计时，继续监听，绝不触发 STT
                        _capture.Reset();
                        _captureStartedAt = Time.unscaledTime;
                        _lastSpeechAt = Time.unscaledTime;
                        _speechConfirmCount = 0;
                    }
                }
            }
        }

        private void BeginCapture()
        {
            if (_capturing)
            {
                return;
            }
            // STT 调试模式：PTT 按下即"正在录音"；FreeTalk 处于监听态（无结果时"正在监听"，
            // 有上次结果时"正在监听：结果"），待语音确认后置"正在录音"
            if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
            {
                if (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value == EVoiceTriggerMode.FreeTalk)
                {
                    MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = string.IsNullOrEmpty(_debugLastResult)
                        ? "正在监听"
                        : $"正在监听：{_debugLastResult}";
                }
                else
                {
                    MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = "正在录音";
                }
            }
            if (!_capture.Begin())
            {
                Notification("麦克风不可用或被占用");
                return;
            }
            // PTT：按下即累积（无预卷需求）；FreeTalk 保持未武装，待语音确认后由 Arm() 置位
            if (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value == EVoiceTriggerMode.PushToTalk)
            {
                _capture.Arm();
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

            // 自由说话：只裁剪到"静音秒数一半"的尾巴——既不让静音过长，也不说完话立即截断
            if (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value == EVoiceTriggerMode.FreeTalk)
            {
                samples = TrimTrailingSilence(samples);
                // 最短时长兜底：异常残留的极短/空音频不发 STT
                if (samples.Length < (int)(_capture.SampleRate * MinFreeTalkSpeechSeconds))
                {
                    samples = Array.Empty<float>();
                }
            }

            // MiyakoCarryServiceAssistantPlugin.Logger.LogInfo($"录音结束：{(samples == null ? 0 : samples.Length) / (float)_capture.SampleRate:F2}s，{(samples == null ? 0 : samples.Length)} 样本");

            if (samples == null || samples.Length == 0)
            {
                // STT 调试模式：未捕获到音频时给出提示，避免"正在录音"状态卡死
                if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
                {
                    SetDebugText("未捕获到音频");
                }
                _state = EVoiceState.Idle;
                return;
            }

            // 保存最近一次录音，供 DEBUG 区"播放录音"按钮回放
            MiyakoCarryServiceAssistantPlugin.LastVoiceSamples = samples;
            MiyakoCarryServiceAssistantPlugin.LastVoiceSampleRate = _capture.SampleRate;
            MiyakoCarryServiceAssistantPlugin.LastVoiceChannels = _capture.Channels;

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

        /// <summary>
        /// 裁剪 FreeTalk 录音尾部的静音（按 VAD 能量阈值以窗口为单位从末尾回退），
        /// 保留最后一个语音窗口之后"静音秒数一半"的尾巴：静音不会太长，也不会说完话立即被截断。
        /// </summary>
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

            // 从末尾回退，找到最后一个语音窗口的结束位置
            int speechEnd = samples.Length;
            while (speechEnd >= window)
            {
                var win = new float[window];
                Array.Copy(samples, speechEnd - window, win, 0, window);
                if (_vad.IsSpeech(_vad.ComputeRms(win)))
                {
                    break;
                }
                speechEnd -= window;
            }

            if (speechEnd <= 0)
            {
                return Array.Empty<float>();
            }

            // 保留 speechEnd 之后"静音秒数一半"的尾巴，其余裁掉
            int keepTail = (int)(_capture.SampleRate * _vad.SilenceSeconds * 0.5f);
            int keep = Math.Min(samples.Length, speechEnd + Math.Max(0, keepTail));
            if (keep <= 0)
            {
                return Array.Empty<float>();
            }
            if (keep >= samples.Length)
            {
                return samples;
            }
            var trimmed = new float[keep];
            Array.Copy(samples, trimmed, keep);
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
                    SetDebugText($"STT 异常：{ex.Message}");
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
                    SetDebugText(stt?.Error ?? Utils.Locales.VOICESTTFAILED.McsLocalized());
                    _state = EVoiceState.Idle;
                    return;
                }
                _pendingTranscribedText = stt?.Text ?? string.Empty;
                _pendingIntent = new LlmIntent { Error = stt?.Error ?? Utils.Locales.VOICESTTFAILED.McsLocalized() };
                _state = EVoiceState.Dispatching;
                return;
            }

            // STT 调试模式：转写文本覆盖写入调试字段；开启"调试识别指令"时自动调用 LLM 识别（只识别不派发）
            if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
            {
                SetDebugText(stt.Text);
                _state = EVoiceState.Idle;
                if (MiyakoCarryServiceAssistantPlugin.LlmDebugAutoEnabled.Value)
                {
                    _ = RunDebugCommandTestAsync(stt.Text, llmSettings, ct);
                }
                return;
            }

            _state = EVoiceState.Interpreting;
            _pendingTranscribedText = stt.Text;

            // 代理/护送类指令：注入"指令菜单选项"（编号+本地化名+距离提示），LLM 据此返回 optionIndex
            var llmText = stt.Text;
            var optionsPrompt = BuildVoiceOptionsPrompt();
            if (!string.IsNullOrEmpty(optionsPrompt))
            {
                llmText = stt.Text + "\n\n" + optionsPrompt;
            }

            LlmIntent intent;
            try
            {
                // 调试辅助：打印实际发送的完整提示词（System Prompt + User Text），便于核对与优化
                MiyakoCarryServiceAssistantPlugin.Logger.LogWarning(
                    "\n=== LLM System Prompt ===\n" + Utils.PromptTemplates.BuildSystemPrompt(llmSettings.SystemPrompt)
                    + "\n=== LLM User Text ===\n" + llmText);
                intent = await _llm.InterpretAsync(llmText, llmSettings, ct).ConfigureAwait(true);
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
                // 识别结果只允许指令：LLM 未识别统一提示，技术错误保留原文便于排查
                feedback = intent.Error == LlmIntent.NotRecognized
                    ? Utils.Locales.VOICENOTRECOGNIZED.McsLocalized()
                    : intent.Error;
            }
            else if (!string.IsNullOrEmpty(intent.CommandName))
            {
                try
                {
                    dispatched = IntentBinder.BindAndDispatch(intent);
                    var localizedCommand = Utils.PromptTemplates.GetLocalizedNames(intent.CommandName);
                    feedback = dispatched < 0
                        ? Utils.Locales.VOICEAIMATTARGET.McsLocalized()
                        : dispatched > 0
                            ? $"已下发 {dispatched} 名护航：{localizedCommand}"
                            : $"无匹配护航成员：{localizedCommand}";
                }
                catch (Exception ex)
                {
                    MiyakoCarryServiceAssistantPlugin.Logger.LogError($"BindAndDispatch 异常：{ex}");
                    feedback = $"派发异常：{ex.Message}";
                }
            }
            else
            {
                feedback = Utils.Locales.VOICENOTRECOGNIZED.McsLocalized();
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

        /// <summary>
        /// STT 调试模式文本输出：FreeTalk 持久化上次结果并显示"正在监听：结果"，
        /// PTT 直接显示原文本（现行为不变）。
        /// </summary>
        private void SetDebugText(string text)
        {
            if (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value == EVoiceTriggerMode.FreeTalk)
            {
                _debugLastResult = text;
                MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = $"正在监听：{text}";
            }
            else
            {
                MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = text;
            }
        }

        /// <summary>
        /// 调试识别指令：用当前 LLM 配置解析转写文本，把实际将会调用的指令情况写入
        /// "识别指令结果"（只识别，不派发）。与正常管线一样注入代理/护送菜单选项。
        /// </summary>
        private async System.Threading.Tasks.Task RunDebugCommandTestAsync(string text, ProviderSettings llmSettings, CancellationToken ct)
        {
            try
            {
                var llmText = text;
                var optionsPrompt = BuildVoiceOptionsPrompt();
                if (!string.IsNullOrEmpty(optionsPrompt))
                {
                    llmText = text + "\n\n" + optionsPrompt;
                }
                var intent = await _llm.InterpretAsync(llmText, llmSettings, ct).ConfigureAwait(true);
                MiyakoCarryServiceAssistantPlugin.LlmDebugAutoResult.Value = FormatDebugIntent(intent);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                MiyakoCarryServiceAssistantPlugin.Logger.LogError($"LLM 调试识别异常：{ex}");
                MiyakoCarryServiceAssistantPlugin.LlmDebugAutoResult.Value = $"错误：{ex.Message}";
            }
        }

        /// <summary>
        /// 构建"指令菜单选项"提示段（代理/护送类）：枚举当前战局菜单子选项，编号+本地化名+距离提示，
        /// 供 LLM 通过 optionIndex 选择目标。战局外/失败/无选项时返回 null（不注入）。
        /// </summary>
        private string BuildVoiceOptionsPrompt()
        {
            try
            {
                if (!GameLoop.Instance.IsVaildGameWorld)
                {
                    return null;
                }
                var options = McsCommandApi.GetVoiceMenuOptions();
                if (options.Count == 0)
                {
                    return null;
                }

                var sb = new StringBuilder();
                sb.AppendLine("[Command options (numbered list) - ONLY relevant for InteractionProxyAction / QuestProxyAction / StationaryWeaponProxyAction / EscortWorld commands. If the player's phrase refers to one of these options (by its name, distance or description), return its optionIndex (1-based); otherwise set optionIndex to null. Do not mention this list otherwise.]");
                for (int i = 0; i < options.Count; i++)
                {
                    var option = options[i];
                    var display = string.IsNullOrEmpty(option.TargetName)
                        ? option.Name
                        : $"{option.Name}（{option.TargetName}）";
                    sb.AppendLine($"{i + 1}. {display}");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                MiyakoCarryServiceAssistantPlugin.Logger.LogWarning($"构建语音选项提示失败：{ex.Message}");
                return null;
            }
        }

        private static bool IsOptionCommand(string commandType)
        {
            return commandType is "InteractionProxyAction" or "QuestProxyAction" or "StationaryWeaponProxyAction" or "EscortWorld";
        }

        /// <summary>格式化 LLM 指令识别结果：未识别 / 技术错误 / 指令名+目标详情 / 无响应。</summary>
        private string FormatDebugIntent(LlmIntent intent)
        {
            if (intent == null || intent.IsError)
            {
                // 识别结果只允许指令：LLM 未识别统一显示，技术错误保留原文便于排查
                return intent != null && intent.Error == LlmIntent.NotRecognized
                    ? Utils.Locales.VOICENOTRECOGNIZED.McsLocalized()
                    : $"错误：{intent?.Error ?? "null"}";
            }
            if (!string.IsNullOrEmpty(intent.CommandName))
            {
                // 代理/护送类且选择了菜单选项：直接显示所选选项名（含距离提示）
                if (intent.OptionIndex.HasValue && IsOptionCommand(intent.CommandName))
                {
                    var options = McsCommandApi.GetVoiceMenuOptions();
                    var idx = intent.OptionIndex.Value - 1;
                    if (idx >= 0 && idx < options.Count)
                    {
                        var option = options[idx];
                        var optionName = string.IsNullOrEmpty(option.TargetName)
                            ? option.Name
                            : $"{option.Name}（{option.TargetName}）";
                        return $"指令：{optionName}";
                    }
                    return $"指令：{Utils.PromptTemplates.GetLocalizedNames(intent.CommandName)}（选项 {intent.OptionIndex}）";
                }

                string detail = string.Empty;
                if (intent.TargetIndices is { Count: > 0 })
                {
                    detail = $"（成员 {string.Join("、", intent.TargetIndices)}）";
                }
                else if (intent.TargetCodeNames is { Count: > 0 })
                {
                    detail = $"（代号 {string.Join("、", intent.TargetCodeNames)}）";
                }
                else
                {
                    switch (intent.Selector)
                    {
                        case EIntentTargetSelector.All:
                            detail = "（全员）";
                            break;
                        case EIntentTargetSelector.ByIndex:
                            detail = $"（成员 {intent.TargetIndex}）";
                            break;
                        case EIntentTargetSelector.ByCodeName:
                            detail = $"（代号 {intent.TargetCodeName}）";
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(intent.AimingBodyPart))
                {
                    detail += $"（{intent.AimingBodyPart}）";
                }
                // 显示本地化权威指令名（TEAM* 系列），与提示词 glossary 同源
                return $"指令：{Utils.PromptTemplates.GetLocalizedNames(intent.CommandName)}{detail}";
            }
            return "无响应";
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