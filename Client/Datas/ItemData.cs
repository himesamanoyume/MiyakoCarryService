

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class ItemData : BaseData
    {
        private WeakReference<Item> _itemRef;
        public Item Item => _itemRef.TryGetTarget(out var item) ? item : null;
        public List<ItemData> ItemsInContainer = null;
        public EItemType ItemType = EItemType.None;
        public Transform RootTransform => GetRootTransfrom();
        protected LootDataMgr _lootDataMgr = MgrAccessor.Get<LootDataMgr>();

        public ItemData(Item item)
        {
            _itemRef = new(item);
            ItemType = ItemViewFactory.GetItemType(item.GetType());
        }

        public abstract void RefreshInteresting(McsAILeadPlayer mcsAILeadPlayer);
        public abstract void UnlockRefreshInteresting(McsAILeadPlayer mcsAILeadPlayer);

        public IEnumerator UnlockRefreshRootItemInteresting(McsAILeadPlayer mcsAILeadPlayer)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
            try
            {
                UnlockRefreshInteresting(mcsAILeadPlayer);
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
            }
        }

        public void UpdateContainerInfoData()
        {
            ItemsInContainer = Item.GetAllDatas().ToList();
        }

        protected abstract Transform GetRootTransfrom();
    }
}