
using System;
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Models;
using UnityEngine;

namespace MiyakoCarryService.Client.Misc
{
    internal class McsAIBossPlayer : AIBossPlayer
    {
        public McsBotPlayerConfig McsBotPlayerConfig;
        public List<WeakReference<Transform>> ExaminedRootTransforms = new();
        public HashSet<LootData> FiltedLootDatas = new();
        public McsAIBossPlayer(Player player, McsBotPlayerConfig mcsBotPlayerConfig) : base(player)
        {
            McsBotPlayerConfig = mcsBotPlayerConfig;
        }

        public void HandleExaminedRootTransform(Transform transform)
        {
            if (transform != null)
            {
                ExaminedRootTransforms.Add(new(transform));
            }
        }

        public bool IsExaminedRootItem(ItemData itemData)
        {
            return GetStillExaminedRootTransforms().Contains(itemData.RootTransform);
        }

        public HashSet<Transform> GetStillExaminedRootTransforms()
        {
            var alives = new HashSet<Transform>();
            var deads = new List<WeakReference<Transform>>();
            foreach (var weakReference in ExaminedRootTransforms)
            {
                if (weakReference.TryGetTarget(out var target))
                {
                    alives.Add(target);
                }
                else
                {
                    deads.Add(weakReference);
                }
            }

            foreach (var dead in deads)
            {
                ExaminedRootTransforms.Remove(dead);
            }

            return alives;
        }
    }
}