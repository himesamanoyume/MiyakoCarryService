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
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Assistant.Mgrs
{
    public class VoiceMgr : BaseMgr
    {
        private AudioCaptureService _capture;
        private VadService _vadService;
        private SttDispatcher _sttDispatcher;
        private LlmDispatcher _llmDispatcher;
        private ESttProvider _sttProvider;
        private ELlmProvider _llmProvider;
        private EVoiceState _voiceState = EVoiceState.Idle;
        private bool _capturing;
        private float _captureStartedAt;
        private CancellationTokenSource _processingCts;
        private float _lastSpeechAt;
        private bool _speechStarted;
        private int _speechConfirmCount;
        private const float MinFreeTalkSpeechSeconds = 0.3f;
        private string _debugLastResult;
        private float _windowPeriodSeconds = 0.05f;
        private float _nextWindowAt;
        private LlmIntent _pendingIntent;
        private string _pendingTranscribedText;

        void Awake()
        {
            _capture = new AudioCaptureService();
            _vadService = new VadService(new VadParams
            {
                EnergyThreshold = MiyakoCarryServiceAssistantPlugin.VoiceVadEnergyThreshold.Value,
                SilenceSeconds = MiyakoCarryServiceAssistantPlugin.VoiceVadSilenceSeconds.Value,
            });
            _sttDispatcher = new SttDispatcher(MiyakoCarryServiceAssistantPlugin.SttProvider.Value);
            _llmDispatcher = new LlmDispatcher(MiyakoCarryServiceAssistantPlugin.LlmProvider.Value);
            _sttProvider = MiyakoCarryServiceAssistantPlugin.SttProvider.Value;
            _llmProvider = MiyakoCarryServiceAssistantPlugin.LlmProvider.Value;
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            _processingCts?.Cancel();
            _capture.Stop();
        }

        public override void OnGameWorldStarted(GameWorldStartedEvent @event)
        {
            base.OnGameWorldStarted(@event);
            _capture.RestartForNewRaid();
            _capturing = false;
            _speechStarted = false;
            _speechConfirmCount = 0;
            _captureStartedAt = 0;
            _lastSpeechAt = 0;
            _voiceState = EVoiceState.Idle;
        }

        public override void OnGameWorldEnded(GameWorldEndedEvent @event)
        {
            base.OnGameWorldEnded(@event);
            _capture.RestartForNewRaid();
            _capturing = false;
            _speechStarted = false;
            _speechConfirmCount = 0;
            _voiceState = EVoiceState.Idle;
        }

        void Update()
        {
            var energyThreshold = MiyakoCarryServiceAssistantPlugin.VoiceVadEnergyThreshold.Value;
            var silenceSeconds = MiyakoCarryServiceAssistantPlugin.VoiceVadSilenceSeconds.Value;
            if (Math.Abs(_vadService.EnergyThreshold - energyThreshold) > 0.0001f || Math.Abs(_vadService.SilenceSeconds - silenceSeconds) > 0.0001f)
            {
                _vadService = new VadService(new VadParams
                {
                    EnergyThreshold = energyThreshold,
                    SilenceSeconds = silenceSeconds,
                });
            }

            var sttProvider = MiyakoCarryServiceAssistantPlugin.SttProvider.Value;
            if (_sttProvider != sttProvider)
            {
                _sttDispatcher = new SttDispatcher(sttProvider);
                _sttProvider = sttProvider;
            }
            var llmProvider = MiyakoCarryServiceAssistantPlugin.LlmProvider.Value;
            if (_llmProvider != llmProvider)
            {
                _llmDispatcher = new LlmDispatcher(llmProvider);
                _llmProvider = llmProvider;
            }

            if (_voiceState == EVoiceState.Dispatching && _pendingIntent != null)
            {
                ConsumePendingIntent();
            }

            if (!MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value && (!MiyakoCarryServiceAssistantPlugin.VoiceEnabled.Value || !GameLoop.Instance.IsVaildGameWorld))
            {
                if (_capturing)
                {
                    _capture.Abort();
                    _capturing = false;
                    _speechStarted = false;
                    _speechConfirmCount = 0;
                }
                _voiceState = EVoiceState.Idle;
                return;
            }

            _capture.Poll();

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

            if (isPressed && !_capturing && _voiceState == EVoiceState.Idle)
            {
                BeginCapture();
            }
            else if (!isPressed && _capturing && _voiceState == EVoiceState.Capturing)
            {
                EndCapture();
            }
            else if (_capturing && _voiceState == EVoiceState.Capturing && Time.unscaledTime - _captureStartedAt > MiyakoCarryServiceAssistantPlugin.VoiceCaptureMaxSeconds.Value)
            {
                EndCapture();
            }
        }

        private void HandleFreeTalk()
        {
            if (_voiceState == EVoiceState.Idle && !_capturing)
            {
                BeginCapture();
                return;
            }

            if (!_capturing)
            {
                return;
            }

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

            float rms = _vadService.ComputeRms(window);
            _vadService.UpdateNoiseFloor(rms);

            if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
            {
                MiyakoCarryServiceAssistantPlugin.VoiceDebugVadText.Value = $"rms={rms:F4} speech={_vadService.IsSpeech(rms)} silence={Time.unscaledTime - _lastSpeechAt:F2}s";
            }

            if (_vadService.IsSpeech(rms))
            {
                _lastSpeechAt = Time.unscaledTime;
                _speechConfirmCount++;
                if (!_speechStarted && _speechConfirmCount >= 2)
                {
                    _speechStarted = true;
                    _capture.Arm();
                    if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
                    {
                        MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = Utils.Locales.VOICE_RECORDING.McsLocalized();
                    }
                }
            }
            else
            {
                _speechConfirmCount = 0;
                if (_speechStarted && _vadService.ShouldStopAfterSilence(rms, Time.unscaledTime - _lastSpeechAt))
                {
                    _speechStarted = false;
                    EndCapture();
                }
                else if (_captureStartedAt > 0 && Time.unscaledTime - _captureStartedAt > MiyakoCarryServiceAssistantPlugin.VoiceCaptureMaxSeconds.Value)
                {
                    if (_speechStarted)
                    {
                        _speechStarted = false;
                        EndCapture();
                    }
                    else
                    {
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

            if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
            {
                if (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value == EVoiceTriggerMode.FreeTalk)
                {
                    MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = string.IsNullOrEmpty(_debugLastResult) ? Utils.Locales.VOICELISTENING.McsLocalized() : string.Format(Utils.Locales.VOICE_LISTENING_RESULT.McsLocalized(), _debugLastResult);
                }
                else
                {
                    MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = Utils.Locales.VOICE_RECORDING.McsLocalized();
                }
            }
            if (!_capture.Begin())
            {
                Notification(Utils.Locales.MIC_UNAVAILABLE.McsLocalized());
                return;
            }

            if (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value == EVoiceTriggerMode.PushToTalk)
            {
                _capture.Arm();
            }
            _capturing = true;
            _captureStartedAt = Time.unscaledTime;
            _speechStarted = false;
            _speechConfirmCount = 0;
            _lastSpeechAt = Time.unscaledTime;
            _voiceState = EVoiceState.Capturing;
        }

        private void EndCapture()
        {
            if (!_capturing)
            {
                return;
            }
            var samples = _capture.End();
            _capturing = false;

            if (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value == EVoiceTriggerMode.FreeTalk)
            {
                samples = TrimTrailingSilence(samples);
                if (samples.Length < (int)(_capture.SampleRate * MinFreeTalkSpeechSeconds))
                {
                    samples = Array.Empty<float>();
                }
            }

            // MiyakoCarryServiceAssistantPlugin.Logger.LogInfo($"录音结束：{(samples == null ? 0 : samples.Length) / (float)_capture.SampleRate:F2}s，{(samples == null ? 0 : samples.Length)} 样本");

            if (samples == null || samples.Length == 0)
            {
                if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
                {
                    SetDebugText(Utils.Locales.NO_AUDIO_CAPTURED.McsLocalized());
                }
                _voiceState = EVoiceState.Idle;
                return;
            }

            MiyakoCarryServiceAssistantPlugin.LastVoiceSamples = samples;
            MiyakoCarryServiceAssistantPlugin.LastVoiceSampleRate = _capture.SampleRate;
            MiyakoCarryServiceAssistantPlugin.LastVoiceChannels = _capture.Channels;

            _processingCts?.Cancel();
            _processingCts = new CancellationTokenSource();
            _ = ProcessCaptureAsync(samples, _processingCts.Token);
        }

        private float[] TrimTrailingSilence(float[] samples)
        {
            if (samples == null || samples.Length == 0)
            {
                return samples;
            }
            var window = (int)(_capture.SampleRate * _windowPeriodSeconds);
            if (window <= 0 || samples.Length < window)
            {
                return samples;
            }

            var speechEnd = samples.Length;
            while (speechEnd >= window)
            {
                var win = new float[window];
                Array.Copy(samples, speechEnd - window, win, 0, window);
                if (_vadService.IsSpeech(_vadService.ComputeRms(win)))
                {
                    break;
                }
                speechEnd -= window;
            }

            if (speechEnd <= 0)
            {
                return Array.Empty<float>();
            }

            var keepTail = (int)(_capture.SampleRate * _vadService.SilenceSeconds * 0.5f);
            var keep = Math.Min(samples.Length, speechEnd + Math.Max(0, keepTail));
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
            _voiceState = EVoiceState.Transcribing;
            var debugOnly = MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value && (!MiyakoCarryServiceAssistantPlugin.VoiceEnabled.Value || !GameLoop.Instance.IsVaildGameWorld);
            var segment = new AudioSegment
            {
                Samples = samples,
                SampleRate = _capture.SampleRate,
                Channels = _capture.Channels,
            };

            var sttSettings = new ProviderSettings
            {
                ApiKey = MiyakoCarryServiceAssistantPlugin.SttApiKey.Value,
                ApiSecret = MiyakoCarryServiceAssistantPlugin.SttApiSecret.Value,
                BaseUrl = MiyakoCarryServiceAssistantPlugin.SttBaseUrl.Value,
                ModelId = MiyakoCarryServiceAssistantPlugin.SttModelId.Value,
                Language = MiyakoCarryServiceAssistantPlugin.SttLanguage.Value,
                TimeoutSec = MiyakoCarryServiceAssistantPlugin.SttTimeoutSec.Value,
            };

            var llmSettings = new ProviderSettings
            {
                ApiKey = MiyakoCarryServiceAssistantPlugin.LlmApiKey.Value,
                ApiSecret = MiyakoCarryServiceAssistantPlugin.LlmApiSecret.Value,
                BaseUrl = MiyakoCarryServiceAssistantPlugin.LlmBaseUrl.Value,
                ModelId = MiyakoCarryServiceAssistantPlugin.LlmModelId.Value,
                SystemPrompt = MiyakoCarryServiceAssistantPlugin.LlmSystemPrompt.Value,
                Temperature = MiyakoCarryServiceAssistantPlugin.LlmTemperature.Value,
                MaxTokens = MiyakoCarryServiceAssistantPlugin.LlmMaxTokens.Value,
                TimeoutSec = MiyakoCarryServiceAssistantPlugin.LlmTimeoutSec.Value,
                ReasoningEffort = MiyakoCarryServiceAssistantPlugin.LlmReasoningEffort.Value,
            };

            SttResult stt;
            try
            {
                stt = await _sttDispatcher.TranscribeAsync(segment, sttSettings, ct).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                MiyakoCarryServiceAssistantPlugin.Logger.LogError($"STT Exception：{ex}");
                if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
                {
                    SetDebugText(string.Format(Utils.Locales.STT_FAILED.McsLocalized(), ex.Message));
                }

                if (debugOnly)
                {
                    _voiceState = EVoiceState.Idle;
                    return;
                }

                _pendingTranscribedText = string.Empty;
                _pendingIntent = new LlmIntent { Error = string.Format(Utils.Locales.STT_FAILED.McsLocalized(), ex.Message) };
                _voiceState = EVoiceState.Dispatching;
                return;
            }

            if (!stt.IsSuccess || string.IsNullOrWhiteSpace(stt.Text))
            {
                if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
                {
                    SetDebugText(stt?.Error ?? Utils.Locales.VOICESTTFAILED.McsLocalized());
                }

                if (debugOnly)
                {
                    _voiceState = EVoiceState.Idle;
                    return;
                }

                _pendingTranscribedText = stt?.Text ?? string.Empty;
                _pendingIntent = new LlmIntent { Error = stt?.Error ?? Utils.Locales.VOICESTTFAILED.McsLocalized() };
                _voiceState = EVoiceState.Dispatching;
                return;
            }

            if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value)
            {
                SetDebugText(stt.Text);
            }

            if (debugOnly)
            {
                if (MiyakoCarryServiceAssistantPlugin.LlmDebugAutoEnabled.Value)
                {
                    _ = RunDebugCommandTestAsync(stt.Text, llmSettings, ct);
                }
                _voiceState = EVoiceState.Idle;
                return;
            }

            _voiceState = EVoiceState.Interpreting;
            _pendingTranscribedText = stt.Text;

            var llmText = stt.Text;
            var optionsPrompt = BuildVoiceOptionsPrompt();
            if (!string.IsNullOrEmpty(optionsPrompt))
            {
                llmText = stt.Text + "\n\n" + optionsPrompt;
            }

            LlmIntent intent;
            try
            {
                intent = await _llmDispatcher.InterpretAsync(llmText, llmSettings, ct).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                MiyakoCarryServiceAssistantPlugin.Logger.LogError($"LLM Exception：{ex}");
                if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value && MiyakoCarryServiceAssistantPlugin.LlmDebugAutoEnabled.Value)
                {
                    MiyakoCarryServiceAssistantPlugin.LlmDebugAutoResult.Value = string.Format(Utils.Locales.LLM_DEBUG_ERROR.McsLocalized(), ex.Message);
                }
                _pendingIntent = new LlmIntent { Error = string.Format(Utils.Locales.LLM_FAILED.McsLocalized(), ex.Message) };
                _voiceState = EVoiceState.Dispatching;
                return;
            }

            _pendingIntent = intent;
            _voiceState = EVoiceState.Dispatching;

            if (MiyakoCarryServiceAssistantPlugin.SttDebugEnabled.Value && MiyakoCarryServiceAssistantPlugin.LlmDebugAutoEnabled.Value)
            {
                MiyakoCarryServiceAssistantPlugin.LlmDebugAutoResult.Value = FormatDebugIntent(intent);
            }
        }

        private void ConsumePendingIntent()
        {
            var intent = _pendingIntent;
            var transcribed = _pendingTranscribedText;
            _pendingIntent = null;
            _pendingTranscribedText = null;
            _voiceState = EVoiceState.Idle;

            if (intent == null)
            {
                return;
            }

            int dispatched = 0;
            string feedback = null;

            if (intent.IsError)
            {
                feedback = intent.Error == LlmIntent.NotRecognized
                    ? Utils.Locales.VOICENOTRECOGNIZED.McsLocalized()
                    : intent.Error;
            }
            else if (!string.IsNullOrEmpty(intent.CommandName))
            {
                try
                {
                    dispatched = Utils.Tools.BindAndDispatch(intent);
                    var localizedCommand = Utils.Tools.GetLocalizedNames(intent.CommandName);
                    feedback = dispatched < 0
                        ? Utils.Locales.VOICEAIMATTARGET.McsLocalized()
                        : (dispatched > 0 ? string.Format(Utils.Locales.DISPATCHED_COUNT.McsLocalized(), dispatched, localizedCommand) : string.Format(Utils.Locales.NO_MATCH_MEMBER.McsLocalized(), localizedCommand));
                }
                catch (Exception ex)
                {
                    MiyakoCarryServiceAssistantPlugin.Logger.LogError($"BindAndDispatch Exception：{ex}");
                    feedback = string.Format(Utils.Locales.DISPATCH_ERROR.McsLocalized(), ex.Message);
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
                State = _voiceState,
                DispatchedMembers = dispatched,
                FeedbackMessage = feedback,
            });

            if (MiyakoCarryServiceAssistantPlugin.VoiceFeedbackNotification.Value && !string.IsNullOrEmpty(feedback))
            {
                Notification(feedback);
            }
        }

        private void SetDebugText(string text)
        {
            if (MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value == EVoiceTriggerMode.FreeTalk)
            {
                _debugLastResult = text;
                MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = string.Format(Utils.Locales.VOICE_LISTENING_RESULT.McsLocalized(), text);
            }
            else
            {
                MiyakoCarryServiceAssistantPlugin.SttDebugText.Value = text;
            }
        }

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
                var intent = await _llmDispatcher.InterpretAsync(llmText, llmSettings, ct).ConfigureAwait(true);
                MiyakoCarryServiceAssistantPlugin.LlmDebugAutoResult.Value = FormatDebugIntent(intent);
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception ex)
            {
                MiyakoCarryServiceAssistantPlugin.Logger.LogError($"LLM Exception：{ex}");
                MiyakoCarryServiceAssistantPlugin.LlmDebugAutoResult.Value = string.Format(Utils.Locales.LLM_DEBUG_ERROR.McsLocalized(), ex.Message);
            }
        }

        private string BuildVoiceOptionsPrompt()
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
                var display = string.IsNullOrEmpty(option.TargetName) ? option.Name : $"{option.Name}（{option.TargetName}）";
                sb.AppendLine($"{i + 1}. {display}");
            }
            return sb.ToString();
        }

        private bool IsOptionCommand(string commandType)
        {
            return commandType is "InteractionProxyAction" or "QuestProxyAction" or "StationaryWeaponProxyAction" or "EscortWorld";
        }

        private string FormatDebugIntent(LlmIntent intent)
        {
            if (intent == null || intent.IsError)
            {
                return intent != null && intent.Error == LlmIntent.NotRecognized ? Utils.Locales.VOICENOTRECOGNIZED.McsLocalized() : string.Format(Utils.Locales.ERROR_PREFIX.McsLocalized(), intent?.Error ?? "null");
            }
            if (!string.IsNullOrEmpty(intent.CommandName))
            {
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
                        return string.Format(Utils.Locales.COMMAND_PREFIX.McsLocalized(), optionName);
                    }
                    return string.Format(Utils.Locales.COMMAND_WITH_OPTION.McsLocalized(), Utils.Tools.GetLocalizedNames(intent.CommandName), intent.OptionIndex);
                }

                string detail = string.Empty;
                if (intent.TargetIndices is { Count: > 0 })
                {
                    detail = string.Format(Utils.Locales.TARGET_INDICES.McsLocalized(), string.Join("、", intent.TargetIndices));
                }
                else if (intent.TargetCodeNames is { Count: > 0 })
                {
                    detail = string.Format(Utils.Locales.TARGET_CODENAMES.McsLocalized(), string.Join("、", intent.TargetCodeNames));
                }
                else
                {
                    switch (intent.Selector)
                    {
                        case EIntentTargetSelector.All:
                            detail = Utils.Locales.ALL_MEMBERS.McsLocalized();
                            break;
                        case EIntentTargetSelector.ByIndex:
                            detail = string.Format(Utils.Locales.TARGET_INDICES.McsLocalized(), intent.TargetIndex);
                            break;
                        case EIntentTargetSelector.ByName:
                            detail = string.Format(Utils.Locales.TARGET_CODENAMES.McsLocalized(), intent.TargetCodeName);
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(intent.AimingBodyPart))
                {
                    detail += $"（{intent.AimingBodyPart}）";
                }
                return string.Format(Utils.Locales.COMMAND_PREFIX.McsLocalized(), Utils.Tools.GetLocalizedNames(intent.CommandName) + detail);
            }
            return Utils.Locales.NO_RESPONSE.McsLocalized();
        }

        private void Notification(string message)
        {
            if (Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance != null)
            {
                NotificationManager.DisplayMessageNotification(message);
            }
        }
    }
}