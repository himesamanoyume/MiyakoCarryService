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

		public static ESideType CurrentType = ESideType.Pmc;

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
					McsLeadPlayerId = session.Profile.Id,
					PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value,
					ArmorLevelThreshold = MiyakoCarryServicePlugin.ArmorLevelThreshold.Value,
					LootingWishlishItem = MiyakoCarryServicePlugin.LootingWishlishItem.Value,
					LootingQuestItem = MiyakoCarryServicePlugin.LootingQuestItem.Value,
					BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value
				});
			}
			CurrentType = raidSettings.Side;
		}
		
		// /// <summary>
		// /// 这可能反而是问题最轻的一种方式了。仅仅只是不要在选了Scav后又切换成Pmc才进图，就不会发生选择Pmc却以Scav进入战局的问题，并且战局设置也能正常生效
		// /// 以下是搭配MainMenuControllerClassPatch并对___eraidMode_0、___raidSettings_0.RaidMode都设为Local时的效果
		// /// 1. 当前不论是直接以Scav组队进入，还是先选Pmc再选Scav，队友都为Scav人物。(符合预期)
		// /// 2. 当前直接以Pmc组队进入，队友都为Pmc人物。(符合预期)
		// /// 3. 唯一的问题：先选Scav再以Pmc进入，会以Scav进入
		// /// </summary>
		// [PatchPostfix]
		// public static void Postfix(ref ERaidMode ___eraidMode_0, ref RaidSettings ___raidSettings_0, ISession session, RaidSettings raidSettings, RaidSettings offlineRaidSettings)
		// {
		// 	___eraidMode_0 = ERaidMode.Local;
		// 	___raidSettings_0.RaidMode = ERaidMode.Local;
		// }
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
