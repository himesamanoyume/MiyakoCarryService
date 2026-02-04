using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MiyakoCarryService.Client.Patches.Events
{
	internal sealed class MatchmakerAcceptScreenShowPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchMakerAcceptScreen), nameof(MatchMakerAcceptScreen.Show), [typeof(ISession), typeof(RaidSettings), typeof(RaidSettings)]);

		/// <summary>
		/// 在准备开始匹配前发送配置文件
		/// </summary>
		[PatchPrefix]
		public static void Prefix(ISession session, RaidSettings raidSettings, RaidSettings offlineRaidSettings)
		{
			if (MiyakoCarryServicePlugin.FikaInstalled)
			{
				_ = McsRequestHandler.UploadMcsBotPlayerConfig(new McsBotPlayerConfig
				{
					McsBossPlayerId = session.Profile.Id,
					PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value,
					ArmorLevelThreshold = MiyakoCarryServicePlugin.ArmorLevelThreshold.Value,
					LootingWishlishItem = MiyakoCarryServicePlugin.LootingWishlishItem.Value,
					LootingQuestItem = MiyakoCarryServicePlugin.LootingQuestItem.Value,
					BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value
				});
			}
		}
		
		/// <summary>
		/// 如果有小队成员，先让其正常加载组队的界面，再将设置调整为本地战局，否则即便调整了战局设置也不会生效
		/// </summary>
		[PatchPostfix]
		public static void Postfix(ref ERaidMode ___eraidMode_0, ISession session, ref RaidSettings raidSettings, RaidSettings offlineRaidSettings)
		{
			___eraidMode_0 = ERaidMode.Local;
			// raidSettings.RaidMode = ERaidMode.Local;
		}
	}

	/// <summary>
	/// 使匹配界面的准备按钮在进行了RaidSettingsLocalPatch后仍可以点击
	/// </summary>
	internal sealed class MatchMakerAcceptScreenReadyPatch : ModulePatch
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
	internal sealed class MatchMakerAcceptScreenCallbackPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchMakerAcceptScreen), nameof(MatchMakerAcceptScreen.method_10));

		[PatchPrefix]
		public static bool Prefix(GroupPlayerViewModelClass player, bool status)
		{
			return false;
		}
	}
}
