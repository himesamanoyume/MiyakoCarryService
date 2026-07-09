using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MiyakoCarryService.Client.Patches.Events
{
	/// <summary>
	/// 使转移时仍能加载护航的小队信息
	/// </summary>
	public sealed class MatchmakerTimeHasComePatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerTimeHasCome), nameof(MatchmakerTimeHasCome.Show), [typeof(IEftSession), typeof(RaidSettings), typeof(MatchmakerPlayersController)]);

		private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

		[PatchPrefix]
		public static void Prefix(IEftSession session, RaidSettings raidSettings, MatchmakerPlayersController matchmaker)
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
