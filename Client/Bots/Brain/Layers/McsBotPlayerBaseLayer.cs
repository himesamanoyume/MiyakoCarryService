using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal abstract class McsBotPlayerBaseLayer<T> : CustomLayer where T : McsBotPlayerBaseLayer<T>
    {
        public McsBotPlayerBaseLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            
        }

        private bool? _isMcsBotPlayer = null;

        public bool IsMcsBotPlayer => _isMcsBotPlayer ??= BotOwner.IsMcsBotPlayer;
        
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
    }
}