
using Comfort.Common;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using MiyakoCarryService.Client.Extensions;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using System;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Patches.Events
{

    /// <summary>
    /// 使实例化新的TraderControllerClass时第一时间更新其Data数据
    /// </summary>
    public sealed class TraderControllerClassConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TraderControllerClass).GetConstructors()[0];

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(TraderControllerClass __instance)
        {
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                var rootItem = __instance.RootItem;
                if (rootItem != null)
                {
                    var rootParentItems = rootItem.GetAllParentItemsAndSelf();
                    var mcsAILeadPlayers = McsMgr.GetAllMcsAILeadPlayer();
                    foreach (var rootParentItem in rootParentItems)
                    {
                        try
                        {
                            var itemData = rootParentItem.GetData();
                            if (itemData == null)
                            {
                                continue;
                            }
                            foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                            {
                                itemData.RefreshRootItemInteresting(mcsAILeadPlayer);
                            }
                        }
                        catch (Exception e)
                        {
                            MiyakoCarryServicePlugin.Logger.LogInfo(e);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 新物品放入触发事件时额外更新其Data数据
    /// </summary>
    public sealed class TraderControllerClassAddItemEventInvokePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(AccessTools.Field(typeof(TraderControllerClass), "action_0").FieldType, "Invoke");

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(object __instance, GEventArgs2 obj)
        {
            var gameloop = GameLoop.Instance;
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                var item = obj.Item;
                var mcsAILeadPlayers = McsMgr.GetAllMcsAILeadPlayer();
                if (item != null)
                {
                    var parentItems = item.GetAllParentItemsAndSelf();
                    foreach (var rootItem in parentItems)
                    {
                        foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                        {
                            rootItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                        }
                    }
                }

                var to = obj.To;
                if (to != null)
                {
                    var toRootItem = to.GetRootItem();
                    if (toRootItem != null)
                    {
                        var toParentRootItems = toRootItem.GetAllParentItemsAndSelf();
                        foreach (var toParentRootItem in toParentRootItems)
                        {
                            foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                            {
                                toParentRootItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 物品离开触发事件时额外更新其Data数据
    /// </summary>
    public sealed class TraderControllerClassRemoveItemEventInvokePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(AccessTools.Field(typeof(TraderControllerClass), "action_1").FieldType, "Invoke");

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(object __instance, GEventArgs3 obj)
        {
            var gameloop = GameLoop.Instance;
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                var item = obj.Item;
                var mcsAILeadPlayers = McsMgr.GetAllMcsAILeadPlayer();
                if (item != null)
                {
                    var parentItems = item.GetAllParentItemsAndSelf();
                    foreach (var rootItem in parentItems)
                    {
                        foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                        {
                            rootItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                        }
                    }
                }

                var from = obj.From;
                if (from != null)
                {
                    var fromRootItem = from.GetRootItem();
                    if (fromRootItem != null)
                    {
                        var fromParentRootItems = fromRootItem.GetAllParentItemsAndSelf();
                        foreach (var fromParentRootItem in fromParentRootItems)
                        {
                            foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                            {
                                fromParentRootItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 物品转移走时额外更新其Data数据
    /// </summary>
    public sealed class TraderControllerClassOutProcessPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TraderControllerClass).GetMethods().FirstOrDefault(m => m.Name == nameof(TraderControllerClass.OutProcess) && m.IsVirtual && m.GetParameters().Length == 5);

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(TraderControllerClass __instance, Item item, ItemAddress from, ItemAddress to, IOperationClass operation, Callback callback)
        {
            var gameloop = GameLoop.Instance;
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                var mcsAILeadPlayers = McsMgr.GetAllMcsAILeadPlayer();
                if (__instance != null)
                {
                    var instanceRootItem = __instance.RootItem;
                    if (instanceRootItem != null)
                    {
                        foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                        {
                            instanceRootItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                        }
                    }
                }

                if (item != null)
                {
                    var rootParentItems = item.GetAllParentItemsAndSelf();
                    foreach (var rootItem in rootParentItems)
                    {
                        foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                        {
                            rootItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                        }
                    }
                }

                if (from != null)
                {
                    var fromRootItem = from.GetRootItem();
                    if (fromRootItem != null)
                    {
                        var fromParentRootItems = fromRootItem.GetAllParentItemsAndSelf();
                        foreach (var fromParentRootItem in fromParentRootItems)
                        {
                            foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                            {
                                fromParentRootItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                            }
                        }
                    }
                }

                if (to != null)
                {
                    var toRootItem = to.GetRootItem();
                    if (toRootItem != null)
                    {
                        var toParentRootItems = toRootItem.GetAllParentItemsAndSelf();
                        foreach (var toParentRootItem in toParentRootItems)
                        {
                            foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                            {
                                toParentRootItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 物品转移进来时额外更新其Data数据
    /// </summary>
    public sealed class TraderControllerClassInProcessPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TraderControllerClass).GetMethods().FirstOrDefault(m => m.Name == nameof(TraderControllerClass.InProcess) && m.IsVirtual && m.GetParameters().Length == 5);

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(TraderControllerClass __instance, Item item, ItemAddress to, bool succeed, IOperationClass operation, Callback callback)
        {
            var gameloop = GameLoop.Instance;
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                var mcsAILeadPlayers = McsMgr.GetAllMcsAILeadPlayer();
                if (__instance != null)
                {
                    var instanceRootItem = __instance.RootItem;
                    if (instanceRootItem != null)
                    {
                        foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                        {
                            instanceRootItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                        }
                    }
                }

                if (item != null)
                {
                    var rootItem = item.GetRootItem();
                    if (rootItem != null)
                    {
                        var rootParentItems = rootItem.GetAllParentItemsAndSelf();
                        foreach (var rootParentItem in rootParentItems)
                        {
                            foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                            {
                                rootParentItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                            }
                        }
                    }
                }

                if (to != null)
                {
                    var toRootItem = to.GetRootItem();
                    if (toRootItem != null)
                    {
                        var toParentRootItems = toRootItem.GetAllParentItemsAndSelf();
                        foreach (var toParentRootItem in toParentRootItems)
                        {
                            foreach (var mcsAILeadPlayer in mcsAILeadPlayers)
                            {
                                toParentRootItem.GetData().DebouncedRefresh(mcsAILeadPlayer);
                            }
                        }
                    }
                }
            }
        }
    }
}
