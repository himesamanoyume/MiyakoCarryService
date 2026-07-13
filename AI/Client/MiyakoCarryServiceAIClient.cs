using BepInEx;
using BepInEx.Configuration;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Enums;
using UnityEngine;

namespace MiyakoCarryService.AI.Client
{
    [BepInPlugin(McsAIGUID, McsAIName, MiyakoCarryServicePlugin.BepInExClientVersion)]
    [BepInProcess(MiyakoCarryServicePlugin.EFTapp)]
    [BepInDependency(MiyakoCarryServicePlugin.BigBrainGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MiyakoCarryServicePlugin.McsGUID, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class MiyakoCarryServiceAIClientPlugin : BaseUnityPlugin
    {
        public const string McsAIGUID = "top.himesamanoyume.miyakocarryservice.ai";
#if DEBUG
        public const string McsAIName = "姫様の夢 MiyakoCarryServiceAI DebugBuild";
#else
        public const string McsAIName = "姫様の夢 MiyakoCarryServiceAI";
#endif

        #region AI

        public static ConfigEntry<bool> EnableVoipCommand;
        public static ConfigEntry<KeyboardShortcut> VoipHotKey;
        public static ConfigEntry<string> LLMBaseUrl;
        public static ConfigEntry<string> LLMApiKey;
        public static ConfigEntry<string> STTBaseUrl;
        public static ConfigEntry<string> STTApiKey;

        #endregion

        void Start()
        {
            SetupConfig();
        }

        private void SetupConfig()
        {
            EnableVoipCommand = McsConfigApi.RegisterConfig(
                EConfigType.AI,
                "开启语音指挥",
                false
            );

            VoipHotKey = McsConfigApi.RegisterConfig(
                EConfigType.AI,
                "下达指示快捷键",
                new KeyboardShortcut()
            );

            LLMBaseUrl = McsConfigApi.RegisterConfig(
                EConfigType.AI,
                "大语言模型服务 BaseUrl",
                ""
            );

            LLMApiKey = McsConfigApi.RegisterConfig(
                EConfigType.AI,
                "大语言模型服务 ApiKey",
                "",
                customAttributes: new ConfigurationManagerAttributes()
                {
                    CustomDrawer = static entry =>
                    {
                        var currentApiKey = (string)entry.BoxedValue;
                        var newApiKey = GUILayout.PasswordField(currentApiKey, '*', GUILayout.ExpandWidth(true));
                        if (newApiKey != currentApiKey)
                        {
                            entry.BoxedValue = newApiKey; 
                        }
                    }
                }
            );

            STTBaseUrl = McsConfigApi.RegisterConfig(
                EConfigType.AI,
                "语音识别服务 BaseUrl",
                ""
            );

            STTApiKey = McsConfigApi.RegisterConfig(
                EConfigType.AI,
                "语音识别服务 ApiKey",
                "",
                customAttributes: new ConfigurationManagerAttributes()
                {
                    CustomDrawer = static entry =>
                    {
                        var currentApiKey = (string)entry.BoxedValue;
                        var newApiKey = GUILayout.PasswordField(currentApiKey, '*', GUILayout.ExpandWidth(true));
                        if (newApiKey != currentApiKey)
                        {
                            entry.BoxedValue = newApiKey; 
                        }
                    }
                }
            );
        }
    }
}