using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal abstract class McsBaseLayer<T> : CustomLayer where T : McsBaseLayer<T>
    {
        public McsBaseLayer(BotOwner botOwner, int priority, McsPlayerData mcsPlayerData) : base(botOwner, priority)
        {
            McsPlayerData = mcsPlayerData;
        }

        public McsPlayerData McsPlayerData;
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