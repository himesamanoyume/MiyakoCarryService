

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Misc;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    internal abstract class ItemData : BaseData
    {
        private WeakReference<Item> _itemRef;
        public Item Item => _itemRef.TryGetTarget(out var item) ? item : null;
        public List<ItemData> ItemsInContainer = null;
        public EItemType ItemType = EItemType.None;
        private WeakReference<Transform> _transformRef;
        public Transform Transform
        {
            get
            {
                if (_transformRef.TryGetTarget(out var transform))
                {
                    return transform;
                }
                else
                {
                    transform = GetTransfrom(); 
                    if (transform != null)
                    {
                        _transformRef = new(transform);
                    }
                    return transform;
                }
            }
        }

        public ItemData(Item item)
        {
            _itemRef = new(item);
            ItemType = ItemViewFactory.GetItemType(item.GetType());
        }

        public abstract void UpdateAllLootInContainerInfo(McsAIBossPlayer mcsAIBossPlayer);

        public IEnumerator UpdateContainerInfoData(McsAIBossPlayer mcsAIBossPlayer)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
            try
            {
                ItemsInContainer = Item.GetAllDatas().ToList();
                UpdateAllLootInContainerInfo(mcsAIBossPlayer);
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogInfo(e);
            }
        }

        protected abstract Transform GetTransfrom();
    }
}