using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;
using System.Reflection;

namespace MiyakoCarryService.Client.Patches.Events
{
	/// <summary>
	/// 使转移时仍能加载护航的小队信息
	/// </summary>
	public sealed class MatchmakerTimeHasComePatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerTimeHasCome), nameof(MatchmakerTimeHasCome.Show));

		private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

		[PatchPrefix]
		public static void Prefix(MatchmakerTimeHasCome.TimeHasComeScreenController controller)
		{
			var session = controller.Session;
			var matchmaker = controller.Matchmaker;

			var profileId = MatchmakerAcceptScreenShowPatch.CurrentType == ESideType.Pmc ? session.Profile.Id : session.ProfileOfPet.Id;

			if (McsMgr.McsTransitBotPlayers.TryGetValue(profileId, out var groupPlayerViewModelClasses))
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
