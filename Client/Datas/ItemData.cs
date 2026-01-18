

using System;
using System.Collections.Generic;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;

namespace MiyakoCarryService.Client.Datas
{
    internal abstract class ItemData : BaseData
    {
        private WeakReference<Item> _itemRef;
        public Item Item
        {
            get
            {
                _itemRef.TryGetTarget(out var item);
                return item;
            }
        }

        public List<ItemData> ItemsInContainer = null;
        public EItemType ItemType = EItemType.None;

        public ItemData(Item item)
        {
            _itemRef = new(item);
            ItemType = ItemViewFactory.GetItemType(item.GetType());
        }

        public abstract void UpdateAllLootInContainerInfo();
    }
}