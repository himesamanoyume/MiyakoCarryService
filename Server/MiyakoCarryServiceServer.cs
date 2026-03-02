using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Services;
using MiyakoCarryService.Server.Patches.Dialogue;
using MiyakoCarryService.Server.Patches.Group;
using MiyakoCarryService.Server.Patches.Friend;
using MiyakoCarryService.Server.Patches.OrderQuest;

namespace MiyakoCarryService.Server
{
    public sealed class MiyakoCarryServiceServer
    {
        [Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
        public sealed class MiyakoCarryServiceServerPreLoad(
            ConfigService configService
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
            }
        }

        [Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
        public sealed class MiyakoCarryServiceServerPostLoad(
            LocaleService localeService, 
            OrderQuestService orderQuestService,
            TraderService traderService,
            ProfileService profileService,
            OrderInfoService orderInfoService,
            CompatibilityService compatibilityService
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
                await Task.Run(() =>
                {
                    var mcsBotPlayerIds = orderInfoService.GetExpiredMcsBotPlayerIds();
                    foreach (var kvp in mcsBotPlayerIds)
                    {
                        profileService.ProcessExpiredMcsBotPlayerProfiles(kvp.Key, kvp.Value);
                    }
                });
            }
        }
    }
}