
using DrakiaXYZ.BigBrain.Brains;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Client.Bots.Brain.Datas
{
    internal class LootingData : CustomLayer.ActionData
    {
        public McsBotPlayerData McsBotPlayerData;
        public LootingType LootingType = LootingType.Price;
    }
}