using System;
using System.Collections.Generic;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal abstract class McsBaseLayer<T>(BotOwner botOwner, int priority) : CustomLayer(botOwner, priority) where T : McsBaseLayer<T>
    {
        private bool? _isMcsBotPlayer = null;

        public bool IsMcsBotPlayer => _isMcsBotPlayer ??= BotOwner.IsMcsBotPlayer;
        protected Dictionary<Type, Func<bool>> _endActionMap;
        
        public McsBotPlayerData McsBotPlayerData
        {
            get
            {
                return field ??= BotOwner.GetMcsBotData();
            }
        }

        private string Name
        {
            get
            {
                return field ??= typeof(T).Name;
            }
        }

        public override string GetName()
        {
            return Name;
        }

        public override bool IsCurrentActionEnding()
        {
            if (CurrentAction == null)
            {
                return true;
            }

            return _endActionMap.TryGetValue(CurrentAction.Type, out var endFunc) ? endFunc() : true;
        }

        protected abstract void InitActionMap();
    }
}