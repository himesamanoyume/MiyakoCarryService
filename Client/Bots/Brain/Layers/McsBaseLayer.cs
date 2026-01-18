using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal abstract class McsBaseLayer<T> : CustomLayer where T : McsBaseLayer<T>
    {
        protected McsBaseLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            
        }

        private bool? _isMcsPlayer = null;

        public bool IsMcsPlayer
        {
            get
            {
                return _isMcsPlayer ??= BotOwner.IsMcsPlayer;
            }
        }
        
        public McsPlayerData McsPlayerData
        {
            get
            {
                return field ??= BotOwner.GetMcsData();
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
    }
}