
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Patches.BigSurvey;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using UI.Matchmaker.Group;

namespace MiyakoCarryService.Client.Patches.Group
{
    /// <summary>
    /// 邀请至队伍界面对玩家添加右键自定义选项
    /// </summary>
    public sealed class GetContextInteractionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerPlayerControllerClass), nameof(MatchmakerPlayerControllerClass.GetContextInteractions));

        private static Traverse _tarkovApplicationTraverse;
        public static bool IsMcsBotPlayerInventoryMode = false;
        public static string McsBotPlayerAid = "";

        [PatchPostfix]
        public static void Postfix(GroupPlayerDataClass player, ContextInteractionsClass __result)
        {
            __result.method_2(
                id: "RefreshFriendList",
                key: Locales.REFRESHFRIENDLIST.McsLocalized(),
                callback: OnRefreshFriendList
            );

            if (IsMcsBotPlayerInventoryMode)
            {
                __result.method_2(
                    id: "BackMainChar",
                    key: Locales.RETURNTOMAINCHAR.McsLocalized(),
                    callback: () => TasksExtensions.HandleExceptions(OnExitMcsBotPlayerInventoryMode(player.AccountId))
                );

                return;
            }

            __result.method_2(
                id: "OpenMcsBotPlayerInventoryMode",
                key: Locales.OPENMCSBOTPLAYERINVENTORY.McsLocalized(),
                callback: () => TasksExtensions.HandleExceptions(OnOpenMcsBotPlayerInventoryMode(player.AccountId))
            );

            __result.method_2(
                id: "SettleMcsOrder",
                key: Locales.SETTLEMCSORDER.McsLocalized(),
                callback: () => OnSettleMcsOrder(player.AccountId)
            );

            __result.method_2(
                id: "RenewMcsOrder",
                key: Locales.RENEWMCSORDER.McsLocalized(),
                callback: () => OnRenewMcsOrder(player.AccountId)
            );
        }

        private static async void OnRefreshFriendList()
        {
            var session = GameLoop.Instance.Session;
            session.SocialNetwork.FriendsList.Clear();
            session.GetFriendsList(result =>
            {
                if (!result.Succeed)
                {
                    return;
                }

                session.SocialNetwork.method_13(result);
                UnityEngine.Object.FindObjectOfType<FriendListInvitePlayerPanel>()?.method_0();
            });
        }

        public static async Task OnExitMcsBotPlayerInventoryMode(string aid)
        {
            await GameLoop.Instance.Session.FlushOperationQueue();

            if (!HasWeaponInEquipmentSlots())
            {
                NotificationManagerClass.DisplayMessageNotification(Locales.RETURNTOMAINCHARNOWEAPON.McsLocalized(), iconType: ENotificationIconType.Alert);
                return;
            }

            if (aid != McsBotPlayerAid)
            {
                NotificationManagerClass.DisplayMessageNotification(Locales.RETURNTOMAINCHARTIP.McsLocalized(), iconType: ENotificationIconType.Alert);
                return;
            }

            if (!McsRequestHandler.RemoveMcsBotPlayerAid(new() { Aid = aid }))
            {
                NotificationManagerClass.DisplayMessageNotification(Locales.RETURNTOMAINCHARTIP.McsLocalized(), iconType: ENotificationIconType.Alert);
                return;
            }

            if (_tarkovApplicationTraverse == null)
            {
                TarkovApplication.Exist(out var tarkovApplication);
                _tarkovApplicationTraverse = Traverse.Create(tarkovApplication);
            }

            var mainMenuControllerClass = _tarkovApplicationTraverse.Field<MainMenuControllerClass>("mainMenuControllerClass").Value;

            McsBotPlayerAid = "";
            IsMcsBotPlayerInventoryMode = false;
            Singleton<PreloaderUI>.Instance.SetLoaderStatus(true);
            await mainMenuControllerClass.method_21();
            EventMgr.Notify(new UpdateProfileEvent());
            EventMgr.Notify(new UpdateMiyakoTraderAssortmentEvent());
            await GameLoop.Instance.Session.RequestBuilds();
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
            await GameLoop.Instance.Session.FlushOperationQueue();
            if (!McsRequestHandler.VerifyMcsBotPlayerAid(new() { Aid = aid }))
            {
                NotificationManagerClass.DisplayMessageNotification(Locales.RETURNTOMAINCHARREFUSE.McsLocalized(), iconType: ENotificationIconType.Alert);
                return;
            }

            if (_tarkovApplicationTraverse == null)
            {
                TarkovApplication.Exist(out var tarkovApplication);
                _tarkovApplicationTraverse = Traverse.Create(tarkovApplication);
            }

            var mainMenuControllerClass = _tarkovApplicationTraverse.Field<MainMenuControllerClass>("mainMenuControllerClass").Value;

            McsBotPlayerAid = aid;
            IsMcsBotPlayerInventoryMode = true;
            Singleton<PreloaderUI>.Instance.SetLoaderStatus(true);
            await mainMenuControllerClass.method_21();
            EventMgr.Notify(new UpdateProfileEvent());
            await GameLoop.Instance.Session.RequestBuilds();
            MenuTaskBarAwakePatch.ShowMcsBotPlayerInventoryModeInfo(true);
        }

        private static void OnSettleMcsOrder(string aid)
        {
            if (!McsRequestHandler.SettleMcsOrder(new() { Aid = aid }))
            {
                NotificationManagerClass.DisplayMessageNotification(Locales.SETTLEMCSORDERREFUSE.McsLocalized(), iconType: ENotificationIconType.Alert);
                return;
            }
            OnRefreshFriendList();
        }

        private static void OnRenewMcsOrder(string aid)
        {
            if (!McsRequestHandler.RenewMcsOrder(new() { Aid = aid }))
            {
                NotificationManagerClass.DisplayMessageNotification(Locales.RENEWMCSORDERREFUSE.McsLocalized(), iconType: ENotificationIconType.Alert);
                return;
            }
            NotificationManagerClass.DisplayMessageNotification(Locales.MIYAKOTRADERORDERNEWQUEST.McsLocalized());
            EventMgr.Notify(new UpdateDailyQuestsEvent());
        }
    }

    /// <summary>
    /// 处于护航库存模式时，隐藏邀请至队伍界面中的其他好友右键选项
    /// </summary>
    public sealed class ContextInteractionsClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ContextInteractionsClass), nameof(ContextInteractionsClass.IsActive));

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