using System.Reflection;
using EFT;
using EFT.Interactive;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Interactive
{
    /// <summary>  
    /// 护航代理破门
    /// </summary>  
    public sealed class DoorGetActionsClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(InteractionContextHelper), nameof(InteractionContextHelper.GetAvailableActions), [typeof(GamePlayerOwner), typeof(Door)]);

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(GamePlayerOwner owner, Door door, ref AvailableInteractionState __result)
        {
            if (door.DoorState != EDoorState.Locked)
            {
                return;
            }

            var doorData = door.GetData();
            if (doorData == null)
            {
                return;
            }

            var mcsBotPlayers = McsMgr.GetAllMyMcsSquadMembers(out var mcsLeadPlayer);
            if (mcsLeadPlayer == null)
            {
                return;
            }
            __result.CurrentActionChanged.Bind(CommandUtils.OnCurrentActionChanged);
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                __result.Actions.Add(new InteractionAction
                {
                    Name = string.Format(Locales.DOORPROXYCOMMAND_NAME.McsLocalized(), mcsBotPlayer.Profile.McsNickname),
                    TargetName = Locales.DOORPROXYCOMMAND_TARGETNAME,
                    Action = () => CommandUtils.Dispatch(
                        ECommandType.InteractionProxyAction.ToString(),
                        [mcsBotPlayer],
                        () => new McsCommandContext { TargetId = doorData.Id() }
                    ),
                    Disabled = !mcsBotPlayer.HealthController.IsAlive
                });
            }
        }
    }

    /// <summary>
    /// 护航代理拾取战利品
    /// </summary>
    public sealed class LootItemGetActionsClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(InteractionContextHelper), nameof(InteractionContextHelper.GetAvailableActions), [typeof(GamePlayerOwner), typeof(LootItem)]);

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(GamePlayerOwner owner, LootItem lootItem, ref AvailableInteractionState __result)
        {
            if (lootItem is Corpse)
            {
                return;
            }

            var itemData = lootItem.Item.GetData();
            if (itemData == null)
            {
                return;
            }

            if (itemData is not LootData lootData)
            {
                return;
            }

            var mcsBotPlayers = McsMgr.GetAllMyMcsSquadMembers(out var mcsLeadPlayer);
            if (mcsLeadPlayer == null)
            {
                return;
            }
            __result.CurrentActionChanged.Bind(CommandUtils.OnCurrentActionChanged);
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                __result.Actions.Add(new InteractionAction
                {
                    Name = string.Format(Locales.LOOTPROXYCOMMAND_NAME.McsLocalized(), mcsBotPlayer.Profile.McsNickname),
                    TargetName = Locales.LOOTPROXYCOMMAND_TARGETNAME,
                    Action = () => CommandUtils.Dispatch(
                        ECommandType.LootProxyAction.ToString(),
                        [mcsBotPlayer],
                        () => new McsCommandContext { TargetId = lootData.Item.Id }
                    ),
                    Disabled = !mcsBotPlayer.HealthController.IsAlive
                });
            }
        }
    }

    /// <summary>  
    /// 护航代理操作固定武器
    /// </summary>  
    public sealed class StationaryWeaponGetActionsClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GetActionsClass), nameof(GetActionsClass.smethod_17));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(GamePlayerOwner owner, StationaryWeapon stationaryWeapon, ref ActionsReturnClass __result)
        {
            var stationaryWeaponData = stationaryWeapon.GetData();
            if (stationaryWeaponData == null)
            {
                return;
            }

            var mcsBotPlayers = McsMgr.GetAllMyMcsSquadMembers(out var mcsLeadPlayer);
            if (mcsLeadPlayer == null)
            {
                return;
            }
            __result.CurrentActionChanged.Bind(CommandUtils.OnCurrentActionChanged);
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                __result.Actions.Add(new ActionsTypesClass
                {
                    Name = Locales.STATIONARYWEAPONPROXYCOMMAND_NAME.McsLocalized() + " " + mcsBotPlayer.Profile.McsNickname,
                    TargetName = Locales.STATIONARYWEAPONPROXYCOMMAND_TARGETNAME,
                    Action = () => CommandUtils.Dispatch(
                        ECommandType.StationaryWeaponProxyAction.ToString(),
                        [mcsBotPlayer],
                        () => new McsCommandContext 
                        { 
                            Position = stationaryWeaponData.GetPos(),
                            TargetId = stationaryWeaponData.Id(),
                        }
                    ),
                    Disabled = !mcsBotPlayer.HealthController.IsAlive
                });
            }
        }
    }
}