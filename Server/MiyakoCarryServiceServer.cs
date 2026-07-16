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
using SPTarkov.Server.Core.Utils;
using System.Collections.Generic;
using MiyakoCarryService.Server.Patches.Profile;
using MiyakoCarryService.Server.Patches.Trader;
using MiyakoCarryService.Server.Utils;
using System.Threading;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Services.Locales;
using System;

namespace MiyakoCarryService.Server
{
    public class MiyakoCarryServiceServer
    {
        [Injectable(TypePriority = OnLoadOrder.Preload)]
        public class MiyakoCarryServiceServerPreLoad(
            IServiceProvider serviceProvider,
            ConfigService configService
        ) : IOnLoad
        {
            public async Task OnLoadAsync(CancellationToken cancellationToken)
            {
                await configService.OnPreLoadAsync();
                new GetClientRepeatableQuestsPatch(serviceProvider).Enable();
                new ChangeRepeatableQuestPatch(serviceProvider).Enable();
                new CompleteQuestPatch(serviceProvider).Enable();
                new GetOtherProfilePatch(serviceProvider).Enable();
                new GetFriendListPatch(serviceProvider).Enable();
                new GameStartPatch(serviceProvider).Enable();
                new SendGroupInvitePatch(serviceProvider).Enable();
                new LeaveGroupPatch(serviceProvider).Enable();
                new RemovePlayerFromGroupPatch(serviceProvider).Enable();
                new EndLocalRaidPatch(serviceProvider).Enable();
                new GetGroupStatusPatch(serviceProvider).Enable();
                new SendLocalisedNpcMessageToPlayerPatch().Enable();
                new GenerateDialogueViewPatch(serviceProvider).Enable();
                new GetDialogByIdFromProfilePatch(serviceProvider).Enable();
                new SaveProfileAsyncPatch(serviceProvider).Enable();
                new GetProfilePatch(serviceProvider).Enable();
                new ItemEventRouterHandleEventsPatch(serviceProvider).Enable();
                new GenerateFleaOffersForTraderPatch(serviceProvider).Enable();
                new GetAssortPatch(serviceProvider).Enable();
                new GetTraderAssortsByTraderIdPatch(serviceProvider).Enable();
                new AddOfferPatch(serviceProvider).Enable();
                new RemovePlayerBuildPatch(serviceProvider).Enable();
                new SaveEquipmentBuildPatch(serviceProvider).Enable();
                new SaveWeaponBuildPatch(serviceProvider).Enable();
                new WebSocketDisconnectPatch(serviceProvider).Enable();

                await Task.CompletedTask;
            }
        }

        [Injectable(TypePriority = OnLoadOrder.PostLoad)]
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
            public async Task OnLoadAsync(CancellationToken cancellationToken)
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
                    infoService.MarkExpiredOrderInfos(profileService.ProcessExpiredMcsBotPlayerNotify);
                    var mcsBotPlayerIds = infoService.GetExpiredMcsBotPlayerIds();
                    foreach (var kvp in mcsBotPlayerIds)
                    {
                        if (profileService.IsMcsBotPlayerInventoryMode(kvp.Key))
                        {
                            continue;
                        }
                        infoService.ProcessExpiredTicketInfo(kvp.Key);
                    }
                });
                _ = CheckForUpdate();
                _ = CheckForIfdianUpdate();

                await Task.CompletedTask;
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
                        var versionPattern = new Regex(@"<p[^>]*id=""Mcs4.1.XLatestVersion""[^>]*>([\s\S]*?)<\/p>", RegexOptions.IgnoreCase);
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