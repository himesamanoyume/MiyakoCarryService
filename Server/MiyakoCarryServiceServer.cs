using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Services;
using MiyakoCarryService.Server.Patches.Dialogue;
using MiyakoCarryService.Server.Patches.Group;
using MiyakoCarryService.Server.Patches.Friend;
using MiyakoCarryService.Server.Patches.OrderQuest;
using System.Net.Http;
using System.Text.RegularExpressions;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Collections.Generic;
using MiyakoCarryService.Server.Patches.Profile;
using MiyakoCarryService.Server.Patches.Trader;
using SPTarkov.Server.Core.Services;
using MiyakoCarryService.Server.Utils;

namespace MiyakoCarryService.Server
{
    public class MiyakoCarryServiceServer
    {
        [Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
        public class MiyakoCarryServiceServerPreLoad(
            ConfigService configService
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
                await configService.OnPreLoadAsync();
                new GetClientRepeatableQuestsPatch().Enable();
                new ChangeRepeatableQuestPatch().Enable();
                new CompleteQuestPatch().Enable();
                new GetOtherProfilePatch().Enable();
                new GetFriendListPatch().Enable();
                new GameStartPatch().Enable();
                new SendGroupInvitePatch().Enable();
                new LeaveGroupPatch().Enable();
                new RemovePlayerFromGroupPatch().Enable();
                new EndLocalRaidPatch().Enable();
                new GetGroupStatusPatch().Enable();
                new SendLocalisedNpcMessageToPlayerPatch().Enable();
                new GenerateDialogueViewPatch().Enable();
                new GetDialogByIdFromProfilePatch().Enable();
                new SptDialogueChatBotPatch().Enable();
                new SaveProfileAsyncPatch().Enable();
                new GetProfilePatch().Enable();
                new ItemEventRouterHandleEventsPatch().Enable();
                new GenerateFleaOffersForTraderPatch().Enable();
                new GetAssortPatch().Enable();
                new GetTraderAssortsByTraderIdPatch().Enable();
                new AddOfferPatch().Enable();
                new RemovePlayerBuildPatch().Enable();
                new SaveEquipmentBuildPatch().Enable();
                new SaveWeaponBuildPatch().Enable();
                new WebSocketDisconnectPatch().Enable();
            }
        }

        [Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
        public class MiyakoCarryServiceServerPostLoad(
            Services.LocaleService localeService,
            QuestService questService,
            TraderService traderService,
            BuildsService buildsService,
            InfoService infoService,
            ProfileService profileService,
            CompatibilityService compatibilityService,
            ConfigService configService,
            InventoryService inventoryService,
            ServerLocalisationService serverLocalisationService,
            ISptLogger<MiyakoCarryServiceServerPostLoad> logger,
            JsonUtil jsonUtil
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
                await localeService.OnPostLoadAsync();
                await traderService.OnPostLoadAsync();
                await buildsService.OnPostLoadAsync();
                await infoService.OnPostLoadAsync();
                await profileService.OnPostLoadAsync();
                await questService.OnPostLoadAsync();
                await compatibilityService.OnPostLoadAsync();
                await inventoryService.OnPostLoadAsync();
                await Task.Run(() =>
                {
                    var mcsBotPlayerIds = infoService.GetExpiredMcsBotPlayerIds();
                    foreach (var kvp in mcsBotPlayerIds)
                    {
                        if (profileService.IsMcsBotPlayerInventoryMode(kvp.Key))
                        {
                            continue;
                        }
                        infoService.ProcessExpiredTicketInfo(kvp.Key);
                        infoService.MarkExpiredOrderInfos();
                    }
                });
                _ = CheckForUpdate();
                _ = CheckForIfdianUpdate();
            }

            private async Task CheckForIfdianUpdate()
            {
                if (!configService.GetMcsPluginConfig().ServerConfig.CheckIfdian)
                {
                    return;
                }

                var ifdian = profileService.GetIfdian();

                if (System.DateTimeOffset.UtcNow - System.DateTimeOffset.FromUnixTimeSeconds(ifdian.Timestamp) <= System.TimeSpan.FromDays(7))
                {
                    return;
                }

                using var httpClient = new HttpClient();
                var proxyUrl = new List<string>(){"https://raw.githubusercontent.com/himesamanoyume/himesamanoyume/", "https://cdn.jsdelivr.net/gh/himesamanoyume/himesamanoyume@", "https://github.tsukiyukimiyako.top/https://raw.githubusercontent.com/himesamanoyume/himesamanoyume/"};
                var ifdianUrl = "refs/heads/main/Ifdian.md";
                try
                {
                    foreach (var url in proxyUrl)
                    {
                        try
                        {
                            var data = await httpClient.GetStringAsync(url + ifdianUrl);
                            var supporter = jsonUtil.Deserialize<List<string>>(data);
                            ifdian.Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            ifdian.Supporter = supporter;
                            break;
                        }
                        catch
                        {
                            
                        }
                    }
                }
                finally
                {
                    profileService.UpdateIfdianName();
                    await profileService.SaveIfdian();
                }
            }

            private async Task CheckForUpdate()
            {
                if (!configService.GetMcsPluginConfig().ServerConfig.CheckUpdate)
                {
                    return;
                }

                var currentVersion = configService.GetClientVersion();
                using var httpClient = new HttpClient();
                var proxyUrl = new List<string>(){"https://raw.githubusercontent.com/himesamanoyume/himesamanoyume/", "https://cdn.jsdelivr.net/gh/himesamanoyume/himesamanoyume@", "https://github.tsukiyukimiyako.top/https://raw.githubusercontent.com/himesamanoyume/himesamanoyume/"};
                var checkUpdateUrl = "refs/heads/main/MiyakoCarryService.md";
                try
                {
                    foreach (var url in proxyUrl)
                    {
                        var data = await httpClient.GetStringAsync(url + checkUpdateUrl);
                        var versionPattern = new Regex(@"<p[^>]*id=""Mcs4.0.XLatestVersion""[^>]*>([\s\S]*?)<\/p>", RegexOptions.IgnoreCase);
                        var match = versionPattern.Match(data);
                        if (match.Success)
                        {
                            var latestVersion = new System.Version(match.Groups[1].Value.Trim());
                            if (latestVersion.CompareTo(currentVersion) > 0)
                            {
                                configService.UpdateLatestVersion(latestVersion);
                                logger.Success(string.Format(serverLocalisationService.GetText(Locales.NEWVERSIONNOTIFY), currentVersion, latestVersion));
                            }
                            break;
                        }
                    }
                }
                catch // (System.Exception e)
                {
                    // logger.Info($"检查更新失败: {e.Message}");
                }
            }
        }
    }
}