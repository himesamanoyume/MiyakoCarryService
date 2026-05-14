using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 处理GameWorld结束时事件，只在非转移的情况下才执行
    /// </summary>
    public sealed class RaidEndedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SessionBackendClass), nameof(SessionBackendClass.LocalRaidEnded));

        [PatchPrefix]
        public static void Prefix(LocalRaidSettings settings, RaidEndDescriptorClass results, FlatItemsDataClass[] lostInsuredItems, Dictionary<string, FlatItemsDataClass[]> transferItems)
        {
            if (results.result != ExitStatus.Transit)
            {
                GameLoop.Instance.IsVaildGameWorld = false;
                GameLoop.Instance.IsGameStarted = false;
            }

            EventMgr.Notify(new GameWorldEndedEvent
            {
                ExitStatus = results.result,
                EndTime = DateTime.Now
            });
        }
    }
}