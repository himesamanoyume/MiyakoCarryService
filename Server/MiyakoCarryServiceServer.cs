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

namespace MiyakoCarryService.Server
{
    public sealed class MiyakoCarryServiceServer
    {
        [Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
        public sealed class MiyakoCarryServiceServerPreLoad(
            ConfigService configService,
            ISptLogger<MiyakoCarryServiceServerPreLoad> logger
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
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
                await configService.OnPreLoadAsync();
                _ = CheckForUpdate();
            }

            private async Task CheckForUpdate()
            {
                if (!configService.GetMiyakoCarryServiceConfig().ServerConfig.CheckUpdate)
                {
                    return;
                }

                var currentVersion = configService.GetClientVersion();
                using var httpClient = new HttpClient();
                try
                {
                    // 这是为了方便中国大陆网络环境无法访问github的妥协方式
                    var data = await httpClient.GetStringAsync("https://gitee.com/himesamanoyume/miyakocarryservice/raw/master/README.md");

                    var versionPattern = new Regex(@"<p[^>]*id=""Mcs4.0.XLatestVersion""[^>]*>([\s\S]*?)<\/p>", RegexOptions.IgnoreCase);
                    var match = versionPattern.Match(data);
                    if (match.Success)
                    {
                        var latestVersion = new System.Version(match.Groups[1].Value.Trim());
                        if (latestVersion.CompareTo(currentVersion) > 0)
                        {
                            logger.Success($"MiyakoCarryService New Version: {currentVersion} ---> {latestVersion}");
                        }
                    }
                }
                catch //(Exception e)
                {
                    // logger.Info($"检查更新失败: {e.Message}");
                }
            }
        }

        [Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
        public sealed class MiyakoCarryServiceServerPostLoad(
            LocaleService localeService,
            QuestService questService,
            TraderService traderService,
            ProfileService profileService,
            InfoService infoService,
            CompatibilityService compatibilityService,
            ConfigService configService,
            InventoryService inventoryService,
            JsonUtil jsonUtil
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
                await localeService.OnPostLoadAsync();
                await traderService.OnPostLoadAsync();
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
                        infoService.ProcessExpiredOrderAndTicketInfo(kvp.Key);
                        profileService.ProcessExpiredMcsBotPlayerProfiles(kvp.Key, kvp.Value);
                    }
                });
                _ = CheckForIfdianUpdate();
            }

            private async Task CheckForIfdianUpdate()
            {
                if (!configService.GetMiyakoCarryServiceConfig().ServerConfig.CheckIfdian)
                {
                    return;
                }

                var ifdian = profileService.GetIfdian();

                if (System.DateTimeOffset.UtcNow - System.DateTimeOffset.FromUnixTimeSeconds(ifdian.Timestamp) <= System.TimeSpan.FromDays(7))
                {
                    return;
                }

                using var httpClient = new HttpClient();
                try
                {
                    var data = await httpClient.GetStringAsync("https://gitee.com/himesamanoyume/afdian/raw/master/README.md");
                    var supporter = jsonUtil.Deserialize<List<string>>(data);
                    ifdian.Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    ifdian.Supporter = supporter;
                    await profileService.SaveIfdian();
                }
                catch
                {
                    
                }
            }
        }
    }
}