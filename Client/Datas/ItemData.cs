

using System;
using System.Collections;
using System.Collections.Generic;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
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
        public Transform RootTransform
        {
            get
            {
                if (_transformRef == null)
                {
                    _transformRef = new(null);
                }

                if (_transformRef.TryGetTarget(out var transform))
                {
                    return transform;
                }
                else
                {
                    transform = GetRootTransfrom(); 
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
            _transformRef = new(GetRootTransfrom());
            ItemType = ItemViewFactory.GetItemType(item.GetType());
        }

        public abstract void RefreshInteresting(McsAILeadPlayer mcsAILeadPlayer);

        public IEnumerator RefreshRootItemInteresting(McsAILeadPlayer mcsAILeadPlayer)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
            try
            {
                RefreshInteresting(mcsAILeadPlayer);
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogInfo(e);
            }
        }

        protected abstract Transform GetRootTransfrom();
    }
}