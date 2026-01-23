
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
            var mcsFullProfileToView = profileController.GetMcsBotPlayerProfileByAccountId(sessionId, request.AccountId);
            if (mcsFullProfileToView is null)
            {
                Console.WriteLine("执行老代码");
                return true;
            }

            var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
            var bossPmcProfile = profileHelper.GetPmcProfile(sessionId);
            var mcsBotPlayerPmcProfile = mcsFullProfileToView.CharacterData.PmcData;
            var mcsBotPlayerScavProfile = mcsFullProfileToView.CharacterData.ScavData;

            var hideoutKeys = new HashSet<string>();
            hideoutKeys.UnionWith(mcsBotPlayerPmcProfile.Inventory.HideoutAreaStashes.Keys);
            hideoutKeys.Add(mcsBotPlayerPmcProfile.Inventory.HideoutCustomizationStashId);

            var hideoutRootItems = mcsBotPlayerPmcProfile.Inventory.Items.Where(x => hideoutKeys.Contains(x.Id));
            var itemsToReturn = new List<Item>();
            foreach (var rootItems in hideoutRootItems)
            {
                var itemWithChildren = mcsBotPlayerPmcProfile.Inventory.Items.GetItemWithChildren(rootItems.Id);
                itemsToReturn.AddRange(itemWithChildren);
            }

            var profile = new GetOtherProfileResponse
            {
                Id = mcsBotPlayerPmcProfile.Id,
                Aid = mcsBotPlayerPmcProfile.Aid,
                Info = new OtherProfileInfo
                {
                    Nickname = mcsBotPlayerPmcProfile.Info.Nickname,
                    Side = mcsBotPlayerPmcProfile.Info.Side,
                    Experience = mcsBotPlayerPmcProfile.Info.Experience,
                    MemberCategory = (int)MemberCategory.Group,
                    BannedState = mcsBotPlayerPmcProfile.Info.BannedState,
                    BannedUntil = mcsBotPlayerPmcProfile.Info.BannedUntil,
                    RegistrationDate = bossPmcProfile is not null ? bossPmcProfile.Info.RegistrationDate : mcsBotPlayerPmcProfile.Info.RegistrationDate,
                },
                Customization = new OtherProfileCustomization
                {
                    Head = mcsBotPlayerPmcProfile.Customization.Head,
                    Body = mcsBotPlayerPmcProfile.Customization.Body,
                    Feet = mcsBotPlayerPmcProfile.Customization.Feet,
                    Hands = mcsBotPlayerPmcProfile.Customization.Hands,
                    Dogtag = mcsBotPlayerPmcProfile.Customization.DogTag,
                    Voice = mcsBotPlayerPmcProfile.Customization.Voice,
                },
                Skills = mcsBotPlayerPmcProfile.Skills,
                Equipment = new OtherProfileEquipment 
                { 
                    Id = mcsBotPlayerPmcProfile.Inventory.Equipment, 
                    Items = mcsBotPlayerPmcProfile.Inventory.Items 
                },
                Achievements = bossPmcProfile is not null ? bossPmcProfile.Achievements : mcsBotPlayerPmcProfile.Achievements,
                FavoriteItems = profileHelper.GetOtherProfileFavorites(mcsBotPlayerPmcProfile),
                PmcStats = new OtherProfileStats
                {
                    Eft = new OtherProfileSubStats
                    {
                        TotalInGameTime = bossPmcProfile is not null ? bossPmcProfile.Stats.Eft.TotalInGameTime : mcsBotPlayerPmcProfile.Stats.Eft.TotalInGameTime,
                        OverAllCounters = bossPmcProfile is not null ? bossPmcProfile.Stats.Eft.OverallCounters : mcsBotPlayerPmcProfile.Stats.Eft.OverallCounters,
                    },
                },
                ScavStats = new OtherProfileStats
                {
                    Eft = new OtherProfileSubStats
                    {
                        TotalInGameTime = bossPmcProfile is not null ? bossPmcProfile.Stats.Eft.TotalInGameTime : mcsBotPlayerPmcProfile.Stats.Eft.TotalInGameTime,
                        OverAllCounters = bossPmcProfile is not null ? bossPmcProfile.Stats.Eft.OverallCounters : mcsBotPlayerPmcProfile.Stats.Eft.OverallCounters,
                    },
                },
                Hideout = mcsBotPlayerPmcProfile.Hideout,
                CustomizationStash = mcsBotPlayerPmcProfile.Inventory.HideoutCustomizationStashId,
                HideoutAreaStashes = mcsBotPlayerPmcProfile.Inventory.HideoutAreaStashes,
                Items = itemsToReturn,
            };

            __result = profile;
            return false;
        }
    }
}