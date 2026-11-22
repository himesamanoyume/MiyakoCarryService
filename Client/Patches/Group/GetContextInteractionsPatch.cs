
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ChatShared;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Matchmaker;
using HarmonyLib;
using Il2CppSystem.Linq;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Extensions.ToIl2Cpp;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Patches.BigSurvey;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;
using UnityEngine;
using SPTushonka.Reflection.Il2Cpp;

namespace MiyakoCarryService.Client.Patches.Group
{
    /// <summary>
    /// 邀请至队伍界面对玩家添加右键自定义选项
    /// </summary>
    public sealed class GetContextInteractionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerPlayersController), nameof(MatchmakerPlayersController.GetContextInteractions));

        public static bool IsMcsBotPlayerInventoryMode = false;
        public static string McsBotPlayerAid = "";

        /// <summary>好友列表刷新是否正在进行（0/1，用 Interlocked 访问）</summary>
        private static int _refreshInFlight;

        [PatchPostfix]
        public static void Postfix(GroupPlayer player, RaidGroupContextInteractions __result)
        {
            __result.AddDynamicInteraction(
                id: "RefreshFriendList",
                key: Locales.REFRESHFRIENDLIST.McsLocalized(),
                callback: new System.Action(RefreshFriendListWithRebuild)
            );

            if (IsMcsBotPlayerInventoryMode)
            {
                __result.AddDynamicInteraction(
                    id: "BackMainChar",
                    key: Locales.RETURNTOMAINCHAR.McsLocalized(),
                    callback: new System.Action(() => OnExitMcsBotPlayerInventoryMode(player.AccountId).McsHandleExceptions())
                );

                return;
            }

            __result.AddDynamicInteraction(
                id: "OpenMcsBotPlayerInventoryMode",
                key: Locales.OPENMCSBOTPLAYERINVENTORY.McsLocalized(),
                callback: new System.Action(() => OnOpenMcsBotPlayerInventoryMode(player.AccountId).McsHandleExceptions())
            );

            __result.AddDynamicInteraction(
                id: "SettleMcsOrder",
                key: Locales.SETTLEMCSORDER.McsLocalized(),
                callback: new System.Action(() => OnSettleMcsOrder(player.AccountId).McsHandleExceptions())
            );

            __result.AddDynamicInteraction(
                id: "RenewMcsOrder",
                key: Locales.RENEWMCSORDER.McsLocalized(),
                callback: new System.Action(() => OnRenewMcsOrder(player.AccountId).McsHandleExceptions())
            );
        }

        public static void RefreshFriendListWithRebuild()
        {
            if (Interlocked.CompareExchange(ref _refreshInFlight, 1, 0) != 0)
            {
                return;
            }

            RefreshFriendListLoopAsync().McsHandleExceptions();
        }

        private static async Task RefreshFriendListLoopAsync()
        {
            try
            {
                var session = GameLoop.Instance.Session;
                var social = session?.SocialNetwork;
                if (social == null)
                {
                    return;
                }

                var result = await session.GetFriendsList().AsManaged();
                if (result == null || !result.Succeed)
                {
                    return;
                }

                var eftFriendsList = social.EftFriendsList;
                if (eftFriendsList == null)
                {
                    return;
                }

                var friends = new Il2CppSystem.Collections.Generic.List<UpdatableChatMember>();
                foreach (var member in result.Value.EftFriends)
                {
                    if (member != null)
                    {
                        friends.Add(member);
                    }
                }

                eftFriendsList.UpdateItems(new Il2CppSystem.Collections.Generic.List<UpdatableChatMember>());
                social.RebuildCombinedFriendsList();
                await Task.Delay(100);
                eftFriendsList.UpdateItems(friends);
                social.RebuildCombinedFriendsList();
            }
            finally
            {
                Interlocked.Exchange(ref _refreshInFlight, 0);
            }
        }

        public static async Task OnExitMcsBotPlayerInventoryMode(string aid)
        {
            await GameLoop.Instance.Session.FlushOperationQueue().AsManaged();

            if (!HasWeaponInEquipmentSlots())
            {
                NotificationManager.DisplayMessageNotification(Locales.RETURNTOMAINCHARNOWEAPON.McsLocalized(), iconType: ENotificationIconType.Alert, textColor: new Il2CppSystem.Nullable<Color>());
                return;
            }

            if (aid != McsBotPlayerAid)
            {
                NotificationManager.DisplayMessageNotification(Locales.RETURNTOMAINCHARTIP.McsLocalized(), iconType: ENotificationIconType.Alert, textColor: new Il2CppSystem.Nullable<Color>());
                return;
            }

            if (!McsRequestHandler.RemoveMcsBotPlayerAid(new() { Aid = aid }))
            {
                NotificationManager.DisplayMessageNotification(Locales.RETURNTOMAINCHARTIP.McsLocalized(), iconType: ENotificationIconType.Alert, textColor: new Il2CppSystem.Nullable<Color>());
                return;
            }

            TarkovApplication.Exist(out var tarkovApplication);

            var menuOperation = tarkovApplication._menuOperation;

            McsBotPlayerAid = "";
            IsMcsBotPlayerInventoryMode = false;
            Singleton<PreloaderUI>.Instance.SetLoaderStatus(true);
            await menuOperation.AfterErrorHandler().AsManaged();
            EventMgr.Notify(new UpdateProfileEvent());
            EventMgr.Notify(new UpdateMiyakoTraderAssortmentEvent());
            await GameLoop.Instance.Session.RequestBuilds().AsManaged();
            MenuTaskBarAwakePatch.ShowMcsBotPlayerInventoryModeInfo(false);
        }

        private static bool HasWeaponInEquipmentSlots()
        {
            try
            {
                var session = GameLoop.Instance.Session;
                if (session == null || session.Profile == null)
                {
                    return true;
                }

                var profile = session.Profile;
                var inventory = profile.Inventory;

                if (inventory == null)
                {
                    return true;
                }

                var equipment = inventory.Equipment;
                if (equipment == null)
                {
                    return true;
                }

                var firstPrimaryWeapon = equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon);
                if (firstPrimaryWeapon != null && firstPrimaryWeapon.Items.Count() > 0)
                {
                    return true;
                }

                var secondPrimaryWeapon = equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon);
                if (secondPrimaryWeapon != null && secondPrimaryWeapon.Items.Count() > 0)
                {
                    return true;
                }

                var holster = equipment.GetSlot(EquipmentSlot.Holster);
                if (holster != null && holster.Items.Count() > 0)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return true;
            }
        }

        private static async Task OnOpenMcsBotPlayerInventoryMode(string aid)
        {
            await GameLoop.Instance.Session.FlushOperationQueue().AsManaged();
            if (!McsRequestHandler.VerifyMcsBotPlayerAid(new() { Aid = aid }))
            {
                NotificationManager.DisplayMessageNotification(Locales.RETURNTOMAINCHARREFUSE.McsLocalized(), iconType: ENotificationIconType.Alert, textColor: new Il2CppSystem.Nullable<Color>());
                return;
            }

            TarkovApplication.Exist(out var tarkovApplication);
            var menuOperation = tarkovApplication._menuOperation;

            McsBotPlayerAid = aid;
            IsMcsBotPlayerInventoryMode = true;
            Singleton<PreloaderUI>.Instance.SetLoaderStatus(true);
            await menuOperation.AfterErrorHandler().AsManaged();
            EventMgr.Notify(new UpdateProfileEvent());
            await GameLoop.Instance.Session.RequestBuilds().AsManaged();
            MenuTaskBarAwakePatch.ShowMcsBotPlayerInventoryModeInfo(true);
        }

        private static async Task OnSettleMcsOrder(string aid)
        {
            if (!McsRequestHandler.SettleMcsOrder(new() { Aid = aid }))
            {
                NotificationManager.DisplayMessageNotification(Locales.SETTLEMCSORDERREFUSE.McsLocalized(), iconType: ENotificationIconType.Alert, textColor: new Il2CppSystem.Nullable<Color>());
                return;
            }
            RefreshFriendListWithRebuild();
        }

        private static async Task OnRenewMcsOrder(string aid)
        {
            if (!McsRequestHandler.RenewMcsOrder(new() { Aid = aid }))
            {
                NotificationManager.DisplayMessageNotification(Locales.RENEWMCSORDERREFUSE.McsLocalized(), iconType: ENotificationIconType.Alert, textColor: new Il2CppSystem.Nullable<Color>());
                return;
            }

            NotificationManager.DisplayMessageNotification(Locales.MIYAKOTRADERORDERNEWQUEST.McsLocalized(), textColor: new Il2CppSystem.Nullable<Color>());
            EventMgr.Notify(new UpdateDailyQuestsEvent());
        }
    }

    /// <summary>
    /// 处于护航库存模式时，隐藏邀请至队伍界面中的其他好友右键选项
    /// </summary>
    public sealed class ContextInteractionsClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RaidGroupContextInteractions), nameof(RaidGroupContextInteractions.IsActive));

        [PatchPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (!GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}