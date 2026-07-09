
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Matchmaker;
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
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerPlayersController), nameof(MatchmakerPlayersController.GetContextInteractions));

        private static Traverse _tarkovApplicationTraverse;
        public static bool IsMcsBotPlayerInventoryMode = false;
        public static string McsBotPlayerAid = "";

        [PatchPostfix]
        public static void Postfix(GroupPlayer player, RaidGroupContextInteractions __result)
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
                    callback: () => OnExitMcsBotPlayerInventoryMode(player.AccountId)
                );

                return;
            }

            __result.method_2(
                id: "OpenMcsBotPlayerInventoryMode",
                key: Locales.OPENMCSBOTPLAYERINVENTORY.McsLocalized(),
                callback: () => OnOpenMcsBotPlayerInventoryMode(player.AccountId)
            );
        }

        private static async void OnRefreshFriendList()
        {
            var session = GameLoop.Instance.Session;
            session.SocialNetwork.SteamOnlyFriendsList.Clear();
            session.GetFriendsList(result =>
            {
                if (!result.Succeed)
                {
                    return;
                }

                session.SocialNetwork.CG_method_13(result);  
                UnityEngine.Object.FindObjectOfType<FriendListInvitePlayerPanel>()?.method_0(); 
            });
        }

        public static async void OnExitMcsBotPlayerInventoryMode(string aid)
        {
            await GameLoop.Instance.Session.FlushOperationQueue();

            if (!HasWeaponInEquipmentSlots())
            {
                NotificationManager.DisplayMessageNotification(Locales.RETURNTOMAINCHARNOWEAPON.McsLocalized(), iconType: ENotificationIconType.Alert);
                return;
            }

            if (aid != McsBotPlayerAid)
            {
                NotificationManager.DisplayMessageNotification(Locales.RETURNTOMAINCHARTIP.McsLocalized(), iconType: ENotificationIconType.Alert);
                return;
            }

            if (!McsRequestHandler.RemoveMcsBotPlayerAid(new() { Aid = aid }))
            {
                NotificationManager.DisplayMessageNotification(Locales.RETURNTOMAINCHARTIP.McsLocalized(), iconType: ENotificationIconType.Alert);
                return;
            }

            if (_tarkovApplicationTraverse == null)
            {
                TarkovApplication.Exist(out var tarkovApplication);
                _tarkovApplicationTraverse = Traverse.Create(tarkovApplication);
            }

            var mainMenuControllerClass = _tarkovApplicationTraverse.Field<MainMenuShowOperation>("_mainMenuShowOperation").Value;

            McsBotPlayerAid = "";
            IsMcsBotPlayerInventoryMode = false;
            Singleton<PreloaderUI>.Instance.SetLoaderStatus(true);
            TasksExtensions.HandleExceptions(mainMenuControllerClass.AfterErrorHandler());
            EventMgr.Notify(new UpdateProfileEvent());
            EventMgr.Notify(new UpdateMiyakoTraderAssortmentEvent());
            TasksExtensions.HandleExceptions(GameLoop.Instance.Session.RequestBuilds());
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

        private static async void OnOpenMcsBotPlayerInventoryMode(string aid)
        {
            await GameLoop.Instance.Session.FlushOperationQueue();
            if (!McsRequestHandler.VerifyMcsBotPlayerAid(new() { Aid = aid }))
            {
                NotificationManager.DisplayMessageNotification(Locales.RETURNTOMAINCHARREFUSE.McsLocalized());
                return;
            }

            if (_tarkovApplicationTraverse == null)
            {
                TarkovApplication.Exist(out var tarkovApplication);
                _tarkovApplicationTraverse = Traverse.Create(tarkovApplication);
            }

            var mainMenuControllerClass = _tarkovApplicationTraverse.Field<MainMenuShowOperation>("_mainMenuShowOperation").Value;

            McsBotPlayerAid = aid;
            IsMcsBotPlayerInventoryMode = true;
            Singleton<PreloaderUI>.Instance.SetLoaderStatus(true);
            TasksExtensions.HandleExceptions(mainMenuControllerClass.AfterErrorHandler());
            EventMgr.Notify(new UpdateProfileEvent());
            TasksExtensions.HandleExceptions(GameLoop.Instance.Session.RequestBuilds());
            MenuTaskBarAwakePatch.ShowMcsBotPlayerInventoryModeInfo(true);
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