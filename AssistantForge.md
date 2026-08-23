## Assistant Addon

Voice-control addon for commanding `McsBotPlayers` with natural language (STT + LLM)

### D. Assistant

- **VoiceEnabled**

Enable Assistant voice command recognition (STT + LLM). Required to use voice control over AI escorts. Defaults to false.

- **VoiceTriggerMode**

Trigger mode: PushToTalk holds a key to talk; FreeTalk auto-records via VAD without key. Defaults to PushToTalk.

- **VoiceHotKey**

Hotkey for PushToTalk mode. Ignored under FreeTalk. Defaults to Empty.

- **VoiceCaptureMaxSeconds**

Maximum single recording length in seconds. Long phrases are cut at this limit. Defaults to 15f.

- **VoiceVadEnergyThreshold**

RMS energy threshold that triggers speech start in FreeTalk. Lower if whispers are ignored. Defaults to 0.01f.

- **VoiceVadSilenceSeconds**

Silence duration to end the recording in FreeTalk. Defaults to 1f.

- **VoiceFeedbackNotification**

Show in-game notification after each voice command is dispatched. Defaults to true.

- **RecordDevice**

Select the recording device used for voice recognition; Default means the system default device. If the list is empty, check your microphone devices in system settings. Defaults to "Default".

- **SttProvider**

Cloud Speech-to-Text provider Defaults to OpenAICompatible.

- **SttApiKey**

API key for the chosen STT provider. Defaults to Empty.

- **SttApiSecret**

Second key (secret/token) for two-key providers (Xfyun/Tencent/Baidu/Volc/Aliyun); leave empty for single-key providers. Defaults to Empty.

- **SttBaseUrl**

Optional custom STT base URL. Defaults to Empty.

- **SttModelId**

Optional STT model name (provider-specific). Leave blank for default. Defaults to Empty.

- **SttLanguage**

Hint language code in BCP-47 (e.g. zh-CN, en-US, ja-JP) for STT. Defaults to Empty.

- **SttTimeoutSec**

Per-request STT timeout in seconds. Defaults to 15.

- **LlmProvider**

Cloud LLM provider; OpenAI-Compatible covers OpenAI / DeepSeek / Moonshot / Ollama / vLLM etc. Defaults to OpenAICompatible.

- **LlmApiKey**

API key for the chosen LLM provider. Defaults to Empty.

- **LlmApiSecret**

Secondary key (Secret/Token) of the Miyako trader's AI provider. Required by two-key providers such as Zhipu (ApiKey=id + ApiSecret=secret) and Spark (ApiKey:ApiSecret); leave empty when using an all-in-one ApiKey. Defaults to Empty.

- **LlmBaseUrl**

Optional custom LLM base URL. Defaults to Empty.

- **LlmModelId**

LLM model name. Defaults to Empty.

- **LlmSystemPrompt**

Optional custom system prompt prepended to Assistant's voice-command instruction template. Defaults to Empty.

- **LlmTemperature**

LLM sampling temperature, 0..2. Lower is more deterministic. Defaults to 0.2.

- **LlmMaxTokens**

Maximum tokens LLM may emit. Affects cost/latency. Defaults to 10107.

- **LlmTimeoutSec**

Per-request LLM timeout in seconds. Defaults to 15.

- **LlmReasoningEffort**

LLM reasoning/thinking effort: none / default / low / medium / high / max. none disables thinking (fast mode); default leaves the model's default behavior; low/medium/high/max enable thinking with increasing effort. Automatically mapped per provider (OpenAI-compatible thinking/reasoning_effort, Claude thinking, Gemini thinkingConfig, Qwen enable_thinking). Unsupported thinking parameters report an error directly without any fallback retry. Defaults to "none".

- **HttpProxyHost**

HTTP proxy hostname or IP used by LLM/STT requests (e.g. 127.0.0.1); empty means direct connection. When configured, all requests (including local addresses) go through the proxy. Defaults to Empty.

- **HttpProxyPort**

HTTP proxy port (e.g. 7890); must be configured together with the proxy host, empty means direct connection. Defaults to Empty.

### Z. Debug

- **SttDebugEnabled**

When enabled, records and transcribes using the current configuration; the transcribed text overwrites the STT debug text field. Defaults to false.

- **SttDebugText**

The latest transcription result of a recording. Defaults to Empty.

- **VoiceDebugVadText**

Live speech detection status for FreeTalk (RMS energy / speech flag / silence timer). Read-only. Defaults to Empty.

- **LlmDebugSend**

Sends the STT debug text to the LLM for testing; the reply or error overwrites the LLM result field. Defaults to false.

- **LlmDebugResult**

The latest reply or error message of an LLM test. Defaults to Empty.

- **LlmDebugAutoEnabled**

When enabled, after STT transcription in debug mode, automatically calls the LLM for command recognition testing; the result is shown in 'Command Recognition Result'. Defaults to false.

- **LlmDebugAutoResult**

The latest automatic command recognition result, showing what command the LLM would actually call. Defaults to Empty.

- **VoiceDebugPlay**

Play back the most recent voice recording (debug playback; requires a successful recording first) Defaults to false.