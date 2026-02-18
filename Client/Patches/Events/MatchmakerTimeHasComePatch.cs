using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MiyakoCarryService.Client.Patches.Events
{
	/// <summary>
	/// 使转移时仍能加载护航的小队信息
	/// </summary>
	internal sealed class MatchmakerTimeHasComePatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerTimeHasCome), nameof(MatchmakerTimeHasCome.Show), [typeof(ISession), typeof(RaidSettings), typeof(MatchmakerPlayerControllerClass)]);

		private static McsMgr McsMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

		[PatchPrefix]
		public static void Prefix(ISession session, RaidSettings raidSettings, MatchmakerPlayerControllerClass matchmaker)
		{
			if (McsMgr.McsTransitBotPlayers.TryGetValue(MatchmakerAcceptScreenShowPatch.CurrentType == ESideType.Pmc ? session.Profile.Id : session.ProfileOfPet.Id, out var groupPlayerViewModelClasses))
			{
				foreach (var groupPlayerViewModelClass in groupPlayerViewModelClasses.Values)
				{
					matchmaker.GroupPlayers.Add(groupPlayerViewModelClass);
				}
				groupPlayerViewModelClasses.Clear();
			}
		}
	}
}
