using System.Reflection;
using EFT;
using EFT.Interactive;
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
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GetActionsClass), nameof(GetActionsClass.smethod_14));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        private static CommandMgr CommandMgr => MgrAccessor.Get<CommandMgr>();

        [PatchPostfix]
        public static void Postfix(GamePlayerOwner owner, Door door, ref ActionsReturnClass __result)
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
                __result.Actions.Add(new ActionsTypesClass
                {
                    Name = string.Format(Locales.DOORPROXYCOMMAND_NAME.McsLocalized(), mcsBotPlayer.Profile.Info.Nickname),
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
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GetActionsClass), nameof(GetActionsClass.smethod_8));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        private static CommandMgr CommandMgr => MgrAccessor.Get<CommandMgr>();

        [PatchPostfix]
        public static void Postfix(GamePlayerOwner owner, LootItem lootItem, ref ActionsReturnClass __result)
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
                __result.Actions.Add(new ActionsTypesClass
                {
                    Name = string.Format(Locales.LOOTPROXYCOMMAND_NAME.McsLocalized(), mcsBotPlayer.Profile.Info.Nickname),
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
}