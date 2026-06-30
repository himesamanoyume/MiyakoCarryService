using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
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

            var mcsBotPlayerIds = CommandMgr.GetMySquadMcsBotPlayerIds();
            __result.CurrentActionChanged.Bind(CommandMgr.OnCurrentActionChanged);
            foreach (var mcsBotPlayerId in mcsBotPlayerIds)
            {
                var mcsBotPlayer = CommandMgr.TryGetMcsBotPlayer(mcsBotPlayerId);
                if (mcsBotPlayer == null || !mcsBotPlayer.HealthController.IsAlive)
                {
                    continue;
                }

                __result.Actions.Add(new ActionsTypesClass
                {
                    Name = string.Format(Locales.DOORPROXYCOMMAND_NAME, mcsBotPlayer.Profile.Nickname),
                    TargetName = Locales.DOORPROXYCOMMAND_TARGETNAME,
                    Action = () => CommandMgr.InteractionProxyActionCommandAction(mcsBotPlayer, doorData),
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

            var mcsBotPlayerIds = CommandMgr.GetMySquadMcsBotPlayerIds();
            __result.CurrentActionChanged.Bind(CommandMgr.OnCurrentActionChanged);
            foreach (var mcsBotPlayerId in mcsBotPlayerIds)
            {
                var mcsBotPlayer = CommandMgr.TryGetMcsBotPlayer(mcsBotPlayerId);
                if (mcsBotPlayer == null || !mcsBotPlayer.HealthController.IsAlive)
                {
                    continue;
                }
                
                __result.Actions.Add(new ActionsTypesClass
                {
                    Name = string.Format(Locales.LOOTPROXYCOMMAND_NAME, mcsBotPlayer.Profile.Nickname),
                    TargetName = Locales.LOOTPROXYCOMMAND_TARGETNAME,
                    Action = () => CommandMgr.LootProxyActionCommandAction(mcsBotPlayer, lootData),
                    Disabled = !mcsBotPlayer.HealthController.IsAlive
                });
            }
        }
    }
}