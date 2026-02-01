
using System.Reflection;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using HarmonyLib;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Loot;

/// <summary>
/// 在被作为战利品掠夺目标时防止因容器上锁而无法拾取
/// </summary>
internal sealed class InteractionsHandlerClassPatch : ModulePatch
{
	private static LootDataMgr LootDataMgr
	{
		get
		{
			return field ??= GameLoop.Instance.GetMgr<LootDataMgr>();
		}
	}
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.smethod_14));

    [PatchPrefix]
    public static bool Prefix(Item item, out Error error, ref bool __result)
    {
		error = null;
		if (item.GetData() is LootData lootData)
		{
			var isLootingTarget = LootDataMgr.IsLootingTarget(lootData);
			if (item.TryGetItemComponent<LockableComponent>(out var lockableComponent) && lockableComponent.Locked)
			{
				error = isLootingTarget ? null : new InteractionsHandlerClass.GClass1593(lockableComponent);
			}
			else if (item.TryGetItemComponent<LockableLootContainerComponent>(out var lockableLootContainerComponent) && lockableLootContainerComponent.IsLocked)
			{
				error = isLootingTarget ? null : new InteractionsHandlerClass.GClass1593(lockableLootContainerComponent);
			}
			__result = error != null;
			return false;
		}
		return true;
    }
}