using Diz.Binding;
using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MiyakoCarryService.Client.Patches.Events
{
	public sealed class MatchmakerAcceptScreenShowPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchMakerAcceptScreen), nameof(MatchMakerAcceptScreen.Show), [typeof(IEftSession), typeof(RaidSettings), typeof(RaidSettings)]);

		public static ESideType CurrentType = ESideType.Pmc;
        public static BindableList<RaidPlayer> GroupPlayers = new();

		/// <summary>
		/// 在准备开始匹配前发送配置文件
		/// </summary>
		[PatchPrefix]
		public static void Prefix(MatchmakerPlayersController ___MatchmakerPlayersController, IEftSession session, RaidSettings raidSettings, RaidSettings offlineRaidSettings)
		{
			if (MiyakoCarryServicePlugin.FikaInstalled)
			{
                TasksExtensions.HandleExceptions(McsRequestHandler.UploadMcsBotPlayerConfig(new McsBotPlayerConfig
				{
					McsLeadPlayerId = session.Profile.Id,
                    EnableLooting = MiyakoCarryServicePlugin.EnableLooting.Value,
					PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value,
					KeywordItemText = MiyakoCarryServicePlugin.KeywordItemText.Value,
					LootingKeywordItem = MiyakoCarryServicePlugin.LootingKeywordItem.Value,
					BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value,
                    EnableKeepFormation = MiyakoCarryServicePlugin.EnableKeepFormation.Value,
                    FormationMatrix = MiyakoCarryServicePlugin.FormationMatrix.Value,
                    FormationSpacing = MiyakoCarryServicePlugin.FormationSpacing.Value,
                    FormationSequentialFill = MiyakoCarryServicePlugin.FormationSequentialFill.Value,
				}));
                GroupPlayers = ___MatchmakerPlayersController.GroupPlayers;
			}
			CurrentType = raidSettings.Side;
		}
	}

	/// <summary>
	/// 使匹配界面的准备按钮在进行了RaidSettingsLocalPatch后仍可以点击
	/// </summary>
	public sealed class MatchMakerAcceptScreenReadyPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchMakerAcceptScreen), nameof(MatchMakerAcceptScreen.MatchingAvailabilityChanged));

		[PatchPrefix]
		public static void Prefix(ref EMatchingStatus matchingStatus)
		{
			matchingStatus = EMatchingStatus.Ready;
		}
	}

	/// <summary>
	/// 如果不Patch该函数，似乎就会导致护航准备完成后立即回退到战局设置界面
	/// </summary>
	public sealed class MatchMakerAcceptScreenCallbackPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchMakerAcceptScreen), nameof(MatchMakerAcceptScreen.RaidReadyStatusChangedHandler));

		[PatchPrefix]
		public static bool Prefix(RaidPlayer player, bool status)
		{
			return false;
		}
	}
}
