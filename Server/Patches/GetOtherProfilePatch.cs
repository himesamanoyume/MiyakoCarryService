
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
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
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SPTarkov.Server.Core.Controllers.ProfileController), nameof(SPTarkov.Server.Core.Controllers.ProfileController.GetOtherProfile));

        [PatchPrefix]
        public static bool Prefix(MongoId sessionId, GetOtherProfileRequest request, ref GetOtherProfileResponse __result)
        {
            var profileController = ServiceLocator.ServiceProvider.GetService<ProfileController>();
            var mcsFullProfileToView = profileController.GetCSFullProfileByAccountId(sessionId, request.AccountId);
            if (mcsFullProfileToView is null)
            {
                Console.WriteLine("执行老代码");
                return true;
            }

            var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
            var bossPmcProfile = profileHelper.GetPmcProfile(sessionId);
            var mcsPlayerPmcProfile = mcsFullProfileToView.CharacterData.PmcData;
            var mcsPlayerScavProfile = mcsFullProfileToView.CharacterData.ScavData;

            var hideoutKeys = new HashSet<string>();
            hideoutKeys.UnionWith(mcsPlayerPmcProfile.Inventory.HideoutAreaStashes.Keys);
            hideoutKeys.Add(mcsPlayerPmcProfile.Inventory.HideoutCustomizationStashId);

            var hideoutRootItems = mcsPlayerPmcProfile.Inventory.Items.Where(x => hideoutKeys.Contains(x.Id));
            var itemsToReturn = new List<Item>();
            foreach (var rootItems in hideoutRootItems)
            {
                var itemWithChildren = mcsPlayerPmcProfile.Inventory.Items.GetItemWithChildren(rootItems.Id);
                itemsToReturn.AddRange(itemWithChildren);
            }

            var profile = new GetOtherProfileResponse
            {
                Id = mcsPlayerPmcProfile.Id,
                Aid = mcsPlayerPmcProfile.Aid,
                Info = new OtherProfileInfo
                {
                    Nickname = mcsPlayerPmcProfile.Info.Nickname,
                    Side = mcsPlayerPmcProfile.Info.Side,
                    Experience = mcsPlayerPmcProfile.Info.Experience,
                    MemberCategory = (int)MemberCategory.Group,
                    BannedState = mcsPlayerPmcProfile.Info.BannedState,
                    BannedUntil = mcsPlayerPmcProfile.Info.BannedUntil,
                    RegistrationDate = bossPmcProfile is not null ? bossPmcProfile.Info.RegistrationDate : mcsPlayerPmcProfile.Info.RegistrationDate,
                },
                Customization = new OtherProfileCustomization
                {
                    Head = mcsPlayerPmcProfile.Customization.Head,
                    Body = mcsPlayerPmcProfile.Customization.Body,
                    Feet = mcsPlayerPmcProfile.Customization.Feet,
                    Hands = mcsPlayerPmcProfile.Customization.Hands,
                    Dogtag = mcsPlayerPmcProfile.Customization.DogTag,
                    Voice = mcsPlayerPmcProfile.Customization.Voice,
                },
                Skills = mcsPlayerPmcProfile.Skills,
                Equipment = new OtherProfileEquipment 
                { 
                    Id = mcsPlayerPmcProfile.Inventory.Equipment, 
                    Items = mcsPlayerPmcProfile.Inventory.Items 
                },
                Achievements = bossPmcProfile is not null ? bossPmcProfile.Achievements : mcsPlayerPmcProfile.Achievements,
                FavoriteItems = profileHelper.GetOtherProfileFavorites(mcsPlayerPmcProfile),
                PmcStats = new OtherProfileStats
                {
                    Eft = new OtherProfileSubStats
                    {
                        TotalInGameTime = bossPmcProfile is not null ? bossPmcProfile.Stats.Eft.TotalInGameTime : mcsPlayerPmcProfile.Stats.Eft.TotalInGameTime,
                        OverAllCounters = bossPmcProfile is not null ? bossPmcProfile.Stats.Eft.OverallCounters : mcsPlayerPmcProfile.Stats.Eft.OverallCounters,
                    },
                },
                ScavStats = new OtherProfileStats
                {
                    Eft = new OtherProfileSubStats
                    {
                        TotalInGameTime = bossPmcProfile is not null ? bossPmcProfile.Stats.Eft.TotalInGameTime : mcsPlayerPmcProfile.Stats.Eft.TotalInGameTime,
                        OverAllCounters = bossPmcProfile is not null ? bossPmcProfile.Stats.Eft.OverallCounters : mcsPlayerPmcProfile.Stats.Eft.OverallCounters,
                    },
                },
                Hideout = mcsPlayerPmcProfile.Hideout,
                CustomizationStash = mcsPlayerPmcProfile.Inventory.HideoutCustomizationStashId,
                HideoutAreaStashes = mcsPlayerPmcProfile.Inventory.HideoutAreaStashes,
                Items = itemsToReturn,
            };

            __result = profile;
            return false;
        }
    }
}