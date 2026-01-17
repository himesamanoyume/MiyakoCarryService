using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal abstract class McsBaseLayer<T>(BotOwner botOwner, int priority) : CustomLayer(botOwner, priority) where T : McsBaseLayer<T>
    {
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