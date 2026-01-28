
using Comfort.Common;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using MiyakoCarryService.Client.Extensions;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using System;

namespace MiyakoCarryService.Client.Patches.Events
{

    /// <summary>
    /// 使实例化新的TraderControllerClass时第一时间更新其Data数据
    /// </summary>
    internal sealed class TraderControllerClassConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TraderControllerClass).GetConstructors()[0];

        private static SquadMgr SquadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPostfix]
        public static void Postfix(TraderControllerClass __instance)
        {
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                var rootItem = __instance.RootItem;
                if (rootItem != null)
                {
                    var rootParentItems = rootItem.GetAllParentItemsAndSelf();
                    var mcsAIBossPlayers = SquadMgr.GetAllMcsAIBossPlayer();
                    foreach (var rootParentItem in rootParentItems)
                    {
                        try
                        {
                            foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                            {
                                rootParentItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer);
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
    internal sealed class TraderControllerClassAddItemEventInvokePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(AccessTools.Field(typeof(TraderControllerClass), "action_0").FieldType, "Invoke");

        private static SquadMgr SquadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPostfix]
        public static void Postfix(object __instance, GEventArgs2 obj)
        {
            var gameloop = GameLoop.Instance;
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                var item = obj.Item;
                var mcsAIBossPlayers = SquadMgr.GetAllMcsAIBossPlayer();
                if (item != null)
                {
                    var parentItems = item.GetAllParentItemsAndSelf();
                    foreach (var rootItem in parentItems)
                    {
                        foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                        {
                            gameloop.StartCoroutine(rootItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
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
                            foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                            {
                                gameloop.StartCoroutine(toParentRootItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
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
    internal sealed class TraderControllerClassRemoveItemEventInvokePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(AccessTools.Field(typeof(TraderControllerClass), "action_1").FieldType, "Invoke");

        private static SquadMgr SquadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPostfix]
        public static void Postfix(object __instance, GEventArgs3 obj)
        {
            var gameloop = GameLoop.Instance;
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                var item = obj.Item;
                var mcsAIBossPlayers = SquadMgr.GetAllMcsAIBossPlayer();
                if (item != null)
                {
                    var parentItems = item.GetAllParentItemsAndSelf();
                    foreach (var rootItem in parentItems)
                    {
                        foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                        {
                            gameloop.StartCoroutine(rootItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
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
                            foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                            {
                                gameloop.StartCoroutine(fromParentRootItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
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
    internal sealed class TraderControllerClassOutProcessPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TraderControllerClass).GetMethods().FirstOrDefault(m => m.Name == nameof(TraderControllerClass.OutProcess) && m.IsVirtual && m.GetParameters().Length == 5);

        private static SquadMgr SquadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPostfix]
        public static void Postfix(TraderControllerClass __instance, Item item, ItemAddress from, ItemAddress to, IOperationClass operation, Callback callback)
        {
            var gameloop = GameLoop.Instance;
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                var mcsAIBossPlayers = SquadMgr.GetAllMcsAIBossPlayer();
                if (__instance != null)
                {
                    var instanceRootItem = __instance.RootItem;
                    if (instanceRootItem != null)
                    {
                        foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                        {
                            gameloop.StartCoroutine(instanceRootItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
                        }
                    }
                }

                if (item != null)
                {
                    var rootParentItems = item.GetAllParentItemsAndSelf();
                    foreach (var rootItem in rootParentItems)
                    {
                        foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                        {
                            gameloop.StartCoroutine(rootItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
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
                            foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                            {
                                gameloop.StartCoroutine(fromParentRootItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
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
                            foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                            {
                                gameloop.StartCoroutine(toParentRootItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
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
    internal sealed class TraderControllerClassInProcessPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TraderControllerClass).GetMethods().FirstOrDefault(m => m.Name == nameof(TraderControllerClass.InProcess) && m.IsVirtual && m.GetParameters().Length == 5);

        private static SquadMgr SquadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPostfix]
        public static void Postfix(TraderControllerClass __instance, Item item, ItemAddress to, bool succeed, IOperationClass operation, Callback callback)
        {
            var gameloop = GameLoop.Instance;
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                var mcsAIBossPlayers = SquadMgr.GetAllMcsAIBossPlayer();
                if (__instance != null)
                {
                    var instanceRootItem = __instance.RootItem;
                    if (instanceRootItem != null)
                    {
                        foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                        {
                            gameloop.StartCoroutine(instanceRootItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
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
                            foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                            {
                                gameloop.StartCoroutine(rootParentItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
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
                            foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                            {
                                gameloop.StartCoroutine(toParentRootItem.GetData().UpdateContainerInfoData(mcsAIBossPlayer));
                            }
                        }
                    }
                }
            }
        }
    }
}
