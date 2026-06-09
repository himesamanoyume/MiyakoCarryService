
using System;
using EFT.InventoryLogic;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class QuestData : TriggerData
    {
        private WeakReference<QuestDataClass> _questRef;
        public QuestDataClass Quest => _questRef.TryGetTarget(out var quest) ? quest : null;
        private WeakReference<Item> _itemRef;
        public Item QuestItem => _itemRef != null ? _itemRef.TryGetTarget(out var questItem) ? questItem : null : null;
        private WeakReference<Transform> _transformRef;
        public Transform QuestTransform => _transformRef.TryGetTarget(out var transform) ? transform : null;

        public QuestData(QuestDataClass quest, Item questItem, Transform questTransform) : base()
        {
            _questRef = new WeakReference<QuestDataClass>(quest);
            _itemRef = new WeakReference<Item>(questItem);
            _transformRef = new WeakReference<Transform>(questTransform);
        }
        public QuestData(QuestDataClass quest, Transform questTransform) : base()
        {
            _questRef = new WeakReference<QuestDataClass>(quest);
            _transformRef = new WeakReference<Transform>(questTransform);
        }
    }
}