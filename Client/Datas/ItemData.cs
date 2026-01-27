

using System;
using System.Collections.Generic;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Datas
{
    internal abstract class ItemData : BaseData
    {
        private WeakReference<Item> _itemRef;
        public Item Item => _itemRef.TryGetTarget(out var item) ? item : null;
        public List<ItemData> ItemsInContainer = null;
        public EItemType ItemType = EItemType.None;
        public List<McsAIBossPlayer> IngoredMcsBossPlayers = new();

        public ItemData(Item item)
        {
            _itemRef = new(item);
            ItemType = ItemViewFactory.GetItemType(item.GetType());
        }

        public abstract void UpdateAllLootInContainerInfo(McsBotPlayerConfig mcsBotPlayerConfig);
    }
}