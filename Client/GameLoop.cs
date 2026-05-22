using Comfort.Common;
using EFT;
using System;
using SPT.Reflection.Utils;
using MiyakoCarryService.Client.Utils;
using System.Collections.Generic;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Mgrs;
using UnityEngine;
using System.IO;
using System.Reflection;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Misc;

namespace MiyakoCarryService.Client
{
    public sealed class GameLoop : MiyakoCarryServiceSingleton<GameLoop>
    {
        public Dictionary<Type, IMgr> Mgrs { get; private set; } = new();
        public Dictionary<string, TraderOffer> ItemBestPriceDict { get; private set; } = new();
        public Shader HighlightShader { get; private set; } = null;
        public Camera MainCamera { get; private set; } = null;
        public Camera OpticCamera { get; private set; } = null;
        public bool IsGameStarted = false;
        private Debouncer<ItemData, McsAILeadPlayer> _updateDebouncer;

        public ISession Session
        {
            get
            {
                return field ??= ClientAppUtils.GetMainApp().GetClientBackEndSession();
            }
        }

        public bool IsVaildGameWorld = false;

        public bool CheckVaildGameWorld()
        {
            IsVaildGameWorld = Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is not HideoutGameWorld && IsGameStarted;
            return IsVaildGameWorld;
        }

        void Update()
        {
            CheckVaildGameWorld();

            if (!IsVaildGameWorld)
            {
                return;
            }

            if (MainCamera == null)
            {
                MainCamera = Camera.main;
            }

            if (OpticCamera == null)
            {
                foreach (var camera in Camera.allCameras)
                {
                    if (camera.name == "BaseOpticCamera(Clone)")
                    {
                        OpticCamera = camera;
                        break;
                    }
                }
            }
        }

        public void LoadAssetBundle()
        {
            if (HighlightShader != null)
            {
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MiyakoCarryService.Client.Assets.miyakocarryservice";
            var highlightShaderName = "assets/shader/teammatehighlight.shader";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return;
                }

                byte[] assetBytes;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    assetBytes = memoryStream.ToArray();
                }

                AssetBundle bundle = AssetBundle.LoadFromMemory(assetBytes);
                if (bundle != null)
                {
                    HighlightShader = bundle.LoadAsset<Shader>(highlightShaderName);
                    if (HighlightShader == null)
                    {
                        Debug.LogException(new Exception($"无法加载Shader: {highlightShaderName}"));
                    }

                    bundle.Unload(false);
                    return;
                }
            }
        }

        public void LoadMcsFika()
        {
            var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assemblyPath = Path.Combine(pluginDir, "Himesamanoyume.MiyakoCarryServiceFika.dll");
            if (!File.Exists(assemblyPath))
            {
                return;
            }

            var assembly = Assembly.LoadFrom(assemblyPath);
            var mcsFikaType = assembly.GetType("MiyakoCarryService.Fika.MiyakoCarryServiceFika");

            if (mcsFikaType != null)
            {
                var mcsFika = Activator.CreateInstance(mcsFikaType);
                var initMethod = mcsFikaType.GetMethod("InitMcsFika");
                initMethod?.Invoke(mcsFika, null);
            }
        }

        public void Init()
        {
            LoadAssetBundle();

            McsMgr.Enable();
            BrainMgr.Enable();
            PlayerDataMgr.Enable();
            LootDataMgr.Enable();
            SubtitlesMgr.Enable();
            CommandMgr.Enable();
            HighlightMgr.Enable();

            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                LoadMcsFika();
            }

            EventMgr.Subscribe<GameWorldStartedEvent>(OnGameWorldStarted, this);  
            EventMgr.Subscribe<GameWorldEndedEvent>(OnGameWorldEnded, this);
        }

        private void OnGameWorldStarted(GameWorldStartedEvent @event)  
        {  
            Reset();  
        }  
    
        private void OnGameWorldEnded(GameWorldEndedEvent @event)  
        {  
            Reset();
            _updateDebouncer.Clear();
            _updateDebouncer = null;
        } 

        private void Reset()
        {
            MainCamera = null;
            OpticCamera = null;
        }

        private void OnDestroy()
        {
            EventMgr.UnsubscribeAll(this); 
        }

        public T GetMgr<T>() where T : IMgr
        {
            return (T)Mgrs[typeof(T)];
        }
        
        public HashSet<T> GetDatas<T, K>() where T : BaseData where K : IMgr
        {
            var mgr = GetMgr<K>();
            return mgr.GetDatas<T>();
        }

        public void DebouncedRefresh(ItemData itemData, McsAILeadPlayer mcsAILeadPlayer)
        {
            if (_updateDebouncer == null)
            {
                _updateDebouncer = new Debouncer<ItemData, McsAILeadPlayer>(
                    this,
                    1f,
                    BatchRefreshItems
                );
            }

            if (_updateDebouncer != null && itemData != null)
            {
                _updateDebouncer.Trigger(itemData, mcsAILeadPlayer);
            }
        }

        private void BatchRefreshItems(Dictionary<ItemData, McsAILeadPlayer> updates)
        {
            foreach (var kvp in updates)
            {
                try
                {
                    kvp.Key.UpdateContainerInfoData();
                    kvp.Key.RefreshRootItemInteresting(kvp.Value);
                }
                catch (Exception e)
                {
                    MiyakoCarryServicePlugin.Logger.LogError($"Batch refresh item error: {e}");
                }
            }
        }
    }
}
