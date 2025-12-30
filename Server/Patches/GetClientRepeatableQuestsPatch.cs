using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace MiyakoCarryService.Server.Patches
{
    public sealed class GetClientRepeatableQuestsPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RepeatableQuestController), nameof(RepeatableQuestController.GetClientRepeatableQuests));
        public static Queue<RepeatableQuest> RepeatableQuestsQueue = new();

        [PatchPostfix]
        public static void Postfix(MongoId sessionID, ref List<PmcDataRepeatableQuest> __result)
        {
            foreach (var repeatableConfig in __result)
            {
                if (repeatableConfig.Name == "Weekly")
                {
                    Console.WriteLine("Weekly");
                    while (RepeatableQuestsQueue.Count > 0)
                    {
                        var quest = RepeatableQuestsQueue.Dequeue();
                        Console.WriteLine("加入新任务");
                        repeatableConfig.ActiveQuests.Add(quest);
                        repeatableConfig.ChangeRequirement.TryAdd(
                            quest.Id,
                            new ChangeRequirement
                            {
                                ChangeCost = quest.ChangeCost,
                                ChangeStandingCost = 0
                            }
                        );
                    }
                    break;
                }
            }
        }
    }
}