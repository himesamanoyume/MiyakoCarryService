
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Misc
{
    public class McsAILeadPlayer : AIBossPlayer
    {
        public McsBotPlayerConfig McsBotPlayerConfig
        {
            get
            {
                if (McsMgr.McsLeadPlayerConfigs.TryGetValue(McsLeadPlayer.ProfileId, out var mcsBotPlayerConfig))
                {
                    return mcsBotPlayerConfig;
                }
                else
                {
                    mcsBotPlayerConfig = new McsBotPlayerConfig
                    {
                        McsLeadPlayerId = McsLeadPlayer.ProfileId,
                        EnableLooting = MiyakoCarryServicePlugin.EnableLooting.Value,
                        PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value,
                        KeywordItemText = MiyakoCarryServicePlugin.KeywordItemText.Value,
                        LootingKeywordItem = MiyakoCarryServicePlugin.LootingKeywordItem.Value,
                        BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value
                    };
                    McsMgr.UpdateMcsBotPlayerConfig(mcsBotPlayerConfig.McsLeadPlayerId, mcsBotPlayerConfig);
                    return mcsBotPlayerConfig;
                }
            }
        }
        public Player McsLeadPlayer;
        public GamePlayerOwner GamePlayerOwner => McsLeadPlayer.GetGamePlayerOwner();
        public McsAILeadPlayer(Player player) : base(player)
        {
            McsLeadPlayer = player;
        }

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        public void CalcGoalEnemy()
        {
            var list = new List<EnemyInfo>();
            var mcsBotPlayerBotOwners = McsMgr.GetAllMcsSquadMembersByMcsLeadId(Player().ProfileId);
            foreach (var botOwner in mcsBotPlayerBotOwners)
            {
                foreach (var enemyInfo in botOwner.EnemiesController.EnemyInfos.Values)
                {
                    if (!enemyInfo.Person.HealthController.IsAlive)
                    {
                        continue;
                    }

                    if (!enemyInfo.IsVisible && enemyInfo.Distance >= 20)
                    {
                        continue;
                    }

                    list.Add(enemyInfo);
                }
            }

            if (list.Count == 0)
            {
                return;
            }

            var closestEnemy = GetClosestEnemy(list);
            if (closestEnemy == null)
            {
                return;
            }

            foreach (var botOwner in mcsBotPlayerBotOwners)
            {
                McsLeadPlayer.BotsGroup.AddEnemy(closestEnemy.Person.AIData.BotOwner, EBotEnemyCause.byKill);
                McsLeadPlayer.BotsGroup.ReportAboutEnemy(closestEnemy.Person.AIData.BotOwner, EEnemyPartVisibleType.Visible, botOwner);
                closestEnemy.IsVisible = true;
                botOwner.Memory.GoalEnemy = closestEnemy;
                closestEnemy.PriorityIndex = 0;
            }
        }

        public EnemyInfo GetClosestEnemy(List<EnemyInfo> enemiesInfos)
        {
            if (enemiesInfos.Count == 0)
            {
                return null;
            }

            EnemyInfo closestEnemy = null;

            var minDistance = Mathf.Infinity;
            foreach (var enemyInfo in enemiesInfos)
            {
                var distance = Position.McsSqrDistance(enemyInfo.CurrPosition);
                if (distance < minDistance)
                {
                    closestEnemy = enemyInfo;
                    minDistance = distance;
                }
            }

            return closestEnemy;
        }
    }
}