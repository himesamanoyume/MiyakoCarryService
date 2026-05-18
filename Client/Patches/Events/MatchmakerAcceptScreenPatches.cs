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
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchMakerAcceptScreen), nameof(MatchMakerAcceptScreen.Show), [typeof(ISession), typeof(RaidSettings), typeof(RaidSettings)]);

		public static ESideType CurrentType = ESideType.Pmc;

		/// <summary>
		/// 在准备开始匹配前发送配置文件
		/// </summary>
		[PatchPrefix]
		public static void Prefix(ISession session, RaidSettings raidSettings, RaidSettings offlineRaidSettings)
		{
			if (MiyakoCarryServicePlugin.FikaInstalled)
			{
                TasksExtensions.HandleExceptions(McsRequestHandler.UploadMcsBotPlayerConfig(new McsBotPlayerConfig
				{
					McsLeadPlayerId = session.Profile.Id,
					PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value,
					ArmorLevelThreshold = MiyakoCarryServicePlugin.ArmorLevelThreshold.Value,
					LootingWishlishItem = MiyakoCarryServicePlugin.LootingWishlishItem.Value,
					LootingQuestItem = MiyakoCarryServicePlugin.LootingQuestItem.Value,
					BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value
				}));
			}
			CurrentType = raidSettings.Side;
		}
	}

	/// <summary>
	/// 使匹配界面的准备按钮在进行了RaidSettingsLocalPatch后仍可以点击
	/// </summary>
	public sealed class MatchMakerAcceptScreenReadyPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchMakerAcceptScreen), nameof(MatchMakerAcceptScreen.method_16));

		[PatchPrefix]
		public static void Prefix(ref EMatchingStatus matchingStatus)
		{
			matchingStatus = EMatchingStatus.Ready;
		}
	}

	/// <summary>
	/// 如果不Patch该函数，就会导致护航准备完成后立即回退到战局设置界面
	/// </summary>
	public sealed class MatchMakerAcceptScreenCallbackPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchMakerAcceptScreen), nameof(MatchMakerAcceptScreen.method_10));

		[PatchPrefix]
		public static bool Prefix(GroupPlayerViewModelClass player, bool status)
		{
			return false;
		}
	}
}
