using System.Linq;
using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 战利品物体生成时第一时间更新其Data信息
    /// </summary>
    internal sealed class RegisterLootPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.RegisterLoot), generics: [typeof(LootItem)]);

        private static SquadMgr _squadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPostfix]
        public static void Postfix(InteractableObject loot)
        {
            if (loot == null)
            {
                return;
            }

            if (GameLoop.Instance.IsVaildGameWorld)
            {
                if (loot is LootItem lootItem)
                {
                    var item = lootItem.Item;
                    if (item == null)
                    {
                        return;
                    }

                    var itemData = item.GetData();
                    if (itemData == null)
                    {
                        return;
                    }

                    try
                    {
                        itemData.ItemsInContainer = itemData.Item.GetAllDatas().ToList();
                        foreach (var mcsAIBossPlayer in _squadMgr.GetAllMcsAIBossPlayer())
                        {
                            itemData.UpdateAllLootInContainerInfo(mcsAIBossPlayer.McsBotPlayerConfig);
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}