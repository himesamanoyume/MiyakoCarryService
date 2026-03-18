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
                await configService.OnPreLoadAsync();
                _ = CheckForUpdate();
            }

            private async Task CheckForUpdate()
            {
                if (!configService.GetMiyakoCarryServiceConfig().ServerConfig.CheckUpdate)
                {
                    return;
                }

                // var currentVersion = new System.Version("0.1.7.0"); // Test
                var currentVersion = configService.GetClientVersion();
                using var httpClient = new HttpClient();
                try
                {
                    var data = await httpClient.GetStringAsync("https://gitee.com/himesamanoyume/miyakocarryservice/raw/master/README.md");

                    var versionPattern = new Regex(@"<p[^>]*id=""Mcs4.0.XLatestVersion""[^>]*>([\s\S]*?)<\/p>", RegexOptions.IgnoreCase);
                    var match = versionPattern.Match(data);
                    if (match.Success)
                    {
                        var latestVersion = new System.Version(match.Groups[1].Value.Trim());
                        // logger.Info("尝试检测更新:" + latestVersion);
                        if (latestVersion.CompareTo(currentVersion) > 0)
                        {
                            logger.Success($"MiyakoCarryService 有新版本: {currentVersion} ---> {latestVersion} | 目前请在Discord频道获取更新");
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
            OrderQuestService orderQuestService,
            TraderService traderService,
            ProfileService profileService,
            OrderInfoService orderInfoService,
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
                await orderInfoService.OnPostLoadAsync();
                await profileService.OnPostLoadAsync();
                await orderQuestService.OnPostLoadAsync();
                await compatibilityService.OnPostLoadAsync();
                await inventoryService.OnPostLoadAsync();
                await Task.Run(() =>
                {
                    var mcsBotPlayerIds = orderInfoService.GetExpiredMcsBotPlayerIds();
                    foreach (var kvp in mcsBotPlayerIds)
                    {
                        profileService.ProcessExpiredMcsBotPlayerProfiles(kvp.Key, kvp.Value);
                    }
                });
                _ = CheckForAfdianUpdate();
            }

            private async Task CheckForAfdianUpdate()
            {
                if (!configService.GetMiyakoCarryServiceConfig().ServerConfig.CheckAfdian)
                {
                    return;
                }

                var afdian = profileService.GetAfdian();

                if (System.DateTimeOffset.UtcNow - System.DateTimeOffset.FromUnixTimeSeconds(afdian.Timestamp) <= System.TimeSpan.FromDays(7))
                {
                    return;
                }

                using var httpClient = new HttpClient();
                try
                {
                    var data = await httpClient.GetStringAsync("https://gitee.com/himesamanoyume/afdian/raw/master/README.md");
                    var supporter = jsonUtil.Deserialize<List<string>>(data);
                    afdian.Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    afdian.Supporter = supporter;
                    await profileService.SaveAfdian();
                }
                catch
                {
                    
                }
            }
        }
    }
}