
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using JetBrains.Annotations;
using SPT.Reflection.Patching;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using System.Linq;

namespace MiyakoCarryService.Client.Patches.Inventory
{
    /// <summary>
    /// 让活着的人在被打开背包时，其装备可见
    /// </summary>
    public sealed class TryFindChangedContainerPatch : ModulePatch
    {
        private static McsMgr McsMgr
        {
            get
            {
                return field ??= MgrAccessor.Get<McsMgr>();
            }
        }

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SearchController), nameof(SearchController.TryFindChangedContainer));

        [PatchPostfix]
        public static void Postfix(ItemAddress address, [CanBeNull] out ItemInfo changedContainer, ref bool __result)
        {
            changedContainer = null;
            if (MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction)
            {
                return;
            }

            var ownerId = address?.GetRootItem()?.Owner?.ID;
            if (string.IsNullOrEmpty(ownerId))
            {
                return;
            }

            var mcsSquad = McsMgr.GetAllMyMcsSquadMembers(out _);
            if (mcsSquad == null || !mcsSquad.Any(player => player.ProfileId == ownerId))
            {
                return;
            }

            __result = false;
        }
    }
}