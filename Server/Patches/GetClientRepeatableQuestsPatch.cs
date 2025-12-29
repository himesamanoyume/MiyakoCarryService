using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches
{
    public sealed class GetClientRepeatableQuestsPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RepeatableQuestController), nameof(RepeatableQuestController.GetClientRepeatableQuests));

        [PatchPostfix]
        public static void Postfix(RepeatableQuestController __instance, MongoId sessionID, ref List<PmcDataRepeatableQuest> __result)
        {
            var configService = ServiceLocator.ServiceProvider.GetService<ConfigService>();
            var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
            var timeUtil = ServiceLocator.ServiceProvider.GetService<TimeUtil>();
            var currentTime = timeUtil.GetTimeStamp();
            var fullProfile = profileHelper.GetFullProfile(sessionID);
            var pmcData = fullProfile.CharacterData.PmcData;
            var orderConfig = configService.GetOrderConfig().OrderQuests.First();
            var repeatableQuestControllerTraverse = Traverse.Create(__instance);
            var generatedOrder = repeatableQuestControllerTraverse.Method("GetRepeatableQuestSubTypeFromProfile", [orderConfig, pmcData]).GetValue<PmcDataRepeatableQuest>();
            if (currentTime < generatedOrder.EndTime - 1)
            {
                __result.Add(generatedOrder);
                return;
            }

            generatedOrder.EndTime = currentTime + orderConfig.ResetTime;
            generatedOrder.InactiveQuests = [];
            repeatableQuestControllerTraverse.Method("GetRepeatableQuestSubTypeFromProfile", [generatedOrder, pmcData]);
        }
    }
}