
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

        public Vector3 ClearAreaCacheCenter;
        public float ClearAreaCacheTime;
        public List<Player> ClearAreaCacheMembers;
        public List<List<Vector3>> ClearAreaCacheSegments;

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        public void CleanupDeadEnemies()
        {
            var mcsBotPlayers = McsMgr.GetAllMcsSquadMembersByMcsLeadId(McsLeadPlayer.ProfileId);
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                var botOwner = mcsBotPlayer?.BotOwner;
                if (botOwner == null || botOwner.EnemiesController == null)
                {
                    continue;
                }

                var deadEnemies = new List<IPlayer>();
                foreach (var kvp in botOwner.EnemiesController.EnemyInfos)
                {
                    if (kvp.Key == null || kvp.Value?.Person == null || kvp.Value.Person.HealthController == null || !kvp.Value.Person.HealthController.IsAlive)
                    {
                        deadEnemies.Add(kvp.Key);
                    }
                }

                foreach (var deadEnemy in deadEnemies)
                {
                    if (botOwner.Memory.GoalEnemy?.Person == deadEnemy)
                    {
                        botOwner.Memory.GoalEnemy = null;
                    }

                    if (botOwner.EnemiesController.EnemyInfos.ContainsKey(deadEnemy))
                    {
                        botOwner.EnemiesController.Remove(deadEnemy);
                    }
                }
            }
        }

        public void CalcGoalEnemy(Player seenEnemy)
        {
            CleanupDeadEnemies();

            if (seenEnemy == null || seenEnemy.AIData?.BotOwner == null)
            {
                return;
            }
            if (!seenEnemy.HealthController.IsAlive)
            {
                return;
            }

            var mcsBotPlayers = McsMgr.GetAllMcsSquadMembersByMcsLeadId(McsLeadPlayer.ProfileId);
            var seenBotOwner = seenEnemy.AIData.BotOwner;

            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                var botOwner = mcsBotPlayer.BotOwner;

                McsLeadPlayer.BotsGroup.AddEnemy(seenBotOwner, EBotEnemyCause.callForHelp2);
                // McsLeadPlayer.BotsGroup.ReportAboutEnemy(seenBotOwner, EEnemyPartVisibleType.Visible, botOwner);

                if (botOwner.EnemiesController.EnemyInfos.TryGetValue(seenEnemy, out var enemyInfo))
                {
                    enemyInfo.IsVisible = true;
                    botOwner.Memory.GoalEnemy = enemyInfo;
                    enemyInfo.PriorityIndex = 0;
                }
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