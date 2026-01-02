
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;

namespace MiyakoCarryService.Server.Patches
{
    public sealed class GetOtherProfilePatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ProfileController), nameof(ProfileController.GetOtherProfile));

        [PatchPrefix]
        public static bool Prefix(MongoId sessionId, GetOtherProfileRequest request, ref GetOtherProfileResponse __result)
        {
            var mcsProfileController = ServiceLocator.ServiceProvider.GetService<MCSProfileController>();
            var csFullProfileToView = mcsProfileController.GetCSFullProfileByAccountId(sessionId, request.AccountId);
            if (csFullProfileToView is null)
            {
                return true;
            }

            var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();

            var csPmcProfile = csFullProfileToView.CharacterData.PmcData;
            var csScavProfile = csFullProfileToView.CharacterData.ScavData;

            var hideoutKeys = new HashSet<string>();
            hideoutKeys.UnionWith(csPmcProfile.Inventory.HideoutAreaStashes.Keys);
            hideoutKeys.Add(csPmcProfile.Inventory.HideoutCustomizationStashId);

            var hideoutRootItems = csPmcProfile.Inventory.Items.Where(x => hideoutKeys.Contains(x.Id));
            var itemsToReturn = new List<Item>();
            foreach (var rootItems in hideoutRootItems)
            {
                var itemWithChildren = csPmcProfile.Inventory.Items.GetItemWithChildren(rootItems.Id);
                itemsToReturn.AddRange(itemWithChildren);
            }

            var profile = new GetOtherProfileResponse
            {
                Id = csPmcProfile.Id,
                Aid = csPmcProfile.Aid,
                Info = new OtherProfileInfo
                {
                    Nickname = csPmcProfile.Info.Nickname,
                    Side = csPmcProfile.Info.Side,
                    Experience = csPmcProfile.Info.Experience,
                    MemberCategory = (int)(csPmcProfile.Info.MemberCategory ?? MemberCategory.Default),
                    BannedState = csPmcProfile.Info.BannedState,
                    BannedUntil = csPmcProfile.Info.BannedUntil,
                    RegistrationDate = csPmcProfile.Info.RegistrationDate,
                },
                Customization = new OtherProfileCustomization
                {
                    Head = csPmcProfile.Customization.Head,
                    Body = csPmcProfile.Customization.Body,
                    Feet = csPmcProfile.Customization.Feet,
                    Hands = csPmcProfile.Customization.Hands,
                    Dogtag = csPmcProfile.Customization.DogTag,
                    Voice = csPmcProfile.Customization.Voice,
                },
                Skills = csPmcProfile.Skills,
                Equipment = new OtherProfileEquipment { Id = csPmcProfile.Inventory.Equipment, Items = csPmcProfile.Inventory.Items },
                Achievements = csPmcProfile.Achievements,
                FavoriteItems = profileHelper.GetOtherProfileFavorites(csPmcProfile),
                PmcStats = new OtherProfileStats
                {
                    Eft = new OtherProfileSubStats
                    {
                        TotalInGameTime = csPmcProfile.Stats.Eft.TotalInGameTime,
                        OverAllCounters = csPmcProfile.Stats.Eft.OverallCounters,
                    },
                },
                ScavStats = new OtherProfileStats
                {
                    Eft = new OtherProfileSubStats
                    {
                        TotalInGameTime = csScavProfile.Stats.Eft.TotalInGameTime,
                        OverAllCounters = csScavProfile.Stats.Eft.OverallCounters,
                    },
                },
                Hideout = csPmcProfile.Hideout,
                CustomizationStash = csPmcProfile.Inventory.HideoutCustomizationStashId,
                HideoutAreaStashes = csPmcProfile.Inventory.HideoutAreaStashes,
                Items = itemsToReturn,
            };

            __result = profile;
            return false;
        }
    }
}