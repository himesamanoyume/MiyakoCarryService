using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.GameWorldEvent
{
    /// <summary>
    /// 战局结束时事件
    /// </summary>
    internal sealed class RaidEndedPatch : ModulePatch
    {
        internal static Action OnGameWorldDestory;
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SessionBackendClass), nameof(SessionBackendClass.LocalRaidEnded));

        [PatchPrefix]
        public static void Prefix(LocalRaidSettings settings, RaidEndDescriptorClass results, FlatItemsDataClass[] lostInsuredItems, Dictionary<string, FlatItemsDataClass[]> transferItems)
        {
            GameLoop.Instance.IsGameStarted = false;
            GameLoop.Instance.IsVaildGameWorld = false;
            OnGameWorldDestory?.Invoke();
        }
    }
}