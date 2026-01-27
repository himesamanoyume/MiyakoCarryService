using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MiyakoCarryService.Client.Patches.Events;

/// <summary>
/// 在准备开始匹配前发送配置文件
/// </summary>
internal sealed class MatchmakerAcceptScreenShowPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchMakerAcceptScreen), nameof(MatchMakerAcceptScreen.Show), [typeof(ISession), typeof(RaidSettings), typeof(RaidSettings)]);

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
}