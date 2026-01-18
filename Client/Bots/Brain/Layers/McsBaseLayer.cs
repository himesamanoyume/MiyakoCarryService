using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal abstract class McsBaseLayer<T> : BaseLogicLayerSimpleAbstractClass where T : McsBaseLayer<T>
    {
        protected McsBaseLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            
        }

        private bool? _isMcsPlayer = null;

        public bool IsMcsPlayer => _isMcsPlayer ??= BotOwner_0.IsMcsPlayer;
        
        public McsPlayerData McsPlayerData
        {
            get
            {
                return field ??= BotOwner_0.GetMcsData();
            }
        }

        private string InternalName
        {
            get
            {
                return field ??= typeof(T).Name;
            }
        }

        public override string Name()
        {
            return InternalName;
        }
    }
}