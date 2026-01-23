using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 避免护航Bot将护航老板当做敌人
    /// </summary>
    internal sealed class AddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotsGroup), nameof(BotsGroup.AddEnemy));

        private static SquadMgr SquadMgr
        { 
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPrefix]
        public static bool Prefix(IPlayer person, EBotEnemyCause cause)
        {
            if (person == null)
            {
                return true;
            }

            if (SquadMgr.IsMcsBossPlayer(person.ProfileId))
            {
                // MiyakoCarryServicePlugin.Logger.LogInfo("正在执行AddEnemy");
                // 玩家每次开枪命中敌人，都会让护航想要将我添加为敌人
                return false;
            }

            return true;
        }
    }
}