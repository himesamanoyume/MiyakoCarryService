
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class BuildsController(
        Services.BuildsService buildsService
    )
    {
        public async Task SaveUserBuilds(MongoId mcsLeadPlayerId, UserBuilds userBuilds)
        {
            await buildsService.SaveUserBuilds(mcsLeadPlayerId, userBuilds);
        }

        public UserBuilds? GetUserBuilds(MongoId mcsLeadPlayerId)
        {
            return buildsService.GetUserBuilds(mcsLeadPlayerId);
        }

        public void ExaminedUserBuildsItem(SptProfile fullProfile, UserBuilds? userBuilds)
        {
            buildsService.ExaminedUserBuildsItem(fullProfile, userBuilds);
        }
    }
}