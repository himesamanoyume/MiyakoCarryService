using System;
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class HighlightMgr : BaseMgr<HighlightMgr>
    {
        private Dictionary<Renderer, Material> _cache = new();
        private CommandBuffer _commandBuffer;
        private bool _mainCameraInitialized = false;
        private bool _opticCameraInitialized = false;
        private Dictionary<Material, List<Renderer>> _materialBatches = new();

        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        public override void Start()
        {
            base.Start();
            _commandBuffer = new CommandBuffer { name = "Mcs Player Highlight Pass" };

            MiyakoCarryServicePlugin.TeammateHighlight.SettingChanged += (object sender, EventArgs e) =>
            {
                if (!_gameloop.IsVaildGameWorld)
                {
                    return;
                }

                if (MiyakoCarryServicePlugin.TeammateHighlight.Value)
                {
                    SwitchShaders();
                }
                else
                {
                    _commandBuffer.Clear();
                }
            };

            EventMgr.Subscribe<GameWorldStartedEvent>(OnGameWorldStarted, this);
            EventMgr.Subscribe<GameWorldEndedEvent>(OnGameWorldEnded, this);
        }

        protected override void OnGameWorldStarted(GameWorldStartedEvent @event)
        {
            base.OnGameWorldStarted(@event);
            Clear();
            _mainCameraInitialized = false;
            _opticCameraInitialized = false;
        }

        protected override void OnGameWorldEnded(GameWorldEndedEvent @event)
        {
            base.OnGameWorldEnded(@event);
            Clear();
            _mainCameraInitialized = false;
            _opticCameraInitialized = false;
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            Clear();
            _mainCameraInitialized = false;
            _opticCameraInitialized = false;
        }

        void Update()
        {
            if (KeyInput.KeyDown(MiyakoCarryServicePlugin.TeammateHighlightHotKey.Value, MiyakoCarryServicePlugin.TeammateHighlight))
            {
                if (!MiyakoCarryServicePlugin.TeammateHighlight.Value)
                {
                    _commandBuffer.Clear();
                }
            }

            if (_gameloop.IsVaildGameWorld)
            {
                if (MiyakoCarryServicePlugin.TeammateHighlight.Value)
                {
                    SwitchShaders();
                }

                if (_gameloop.MainCamera != null && !_mainCameraInitialized)
                {
                    _mainCameraInitialized = true;
                    _gameloop.MainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);
                    _gameloop.MainCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);
                }

                if (_gameloop.OpticCamera != null && !_opticCameraInitialized)
                {
                    _opticCameraInitialized = true;
                    _gameloop.OpticCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);
                    _gameloop.OpticCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);
                }
            }

        }

        void SwitchShaders()
        {
            if (!_gameloop.IsVaildGameWorld)
            {
                return;
            }

            _commandBuffer.Clear();
            foreach (var batches in _materialBatches.Values)
            {
                batches.Clear();
            }

            foreach (var playerData in _gameloop.GetDatas<PlayerData, PlayerDataMgr>())
            {
                if (playerData.Player.IsYourPlayer)
                {
                    continue;
                }

                var mcsBotPlayerIds = McsMgr.GetAllMcsBotPlayerIdInRaid();

                if (!mcsBotPlayerIds.Contains(playerData.Player.ProfileId))
                {
                    continue;
                }

                BatchRenderers(playerData.Player, _gameloop.HighlightShader, MiyakoCarryServicePlugin.TeammateHighlightColor.Value.linear);
            }

            ExecuteBatchRendering();
        }

        private void BatchRenderers(Player player, Shader shader, Color color)
        {
            var playerBody = player.PlayerBody;
            if (playerBody == null)
            {
                return;
            }

            var skins = playerBody.BodySkins;
            if (skins == null)
            {
                return;
            }

            foreach (var skin in skins.Values)
            {
                if (skin == null)
                {
                    continue;
                }

                foreach (var renderer in skin.GetRenderers())
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (!renderer.isVisible)
                    {
                        continue;
                    }

                    if (!_cache.TryGetValue(renderer, out var material))
                    {
                        material = new Material(shader);
                        _cache[renderer] = material;
                    }

                    material.SetColor("_HighlightColor", color);
                    material.SetFloat("_HighlightOutlinesWidth", 0.01f);

                    if (!_materialBatches.ContainsKey(material))
                    {
                        _materialBatches[material] = new List<Renderer>();
                    }
                    _materialBatches[material].Add(renderer);
                }
            }
        }

        private void ExecuteBatchRendering()
        {
            foreach (var batch in _materialBatches)
            {
                var material = batch.Key;
                var renderers = batch.Value;

                foreach (var renderer in renderers)
                {
                    if (renderer != null && renderer.isVisible)
                    {
                        _commandBuffer.DrawRenderer(renderer, material, 0, -1);
                    }
                }
            }
        }

        private void Clear()
        {
            foreach (var material in _cache.Values)
            {
                if (material != null)
                {
                    if (material is UnityEngine.Object obj && obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }
            foreach (var batch in _materialBatches)
            {
                batch.Value.Clear();
            }
            _cache.Clear();
            _materialBatches.Clear();
            if (_gameloop.MainCamera != null && _commandBuffer != null)
            {
                _gameloop.MainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);
            }

            if (_gameloop.OpticCamera != null && _commandBuffer != null)
            {
                _gameloop.OpticCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);
            }
        }
    }
}
