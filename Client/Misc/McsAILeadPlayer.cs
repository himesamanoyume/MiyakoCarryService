
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
        /// <summary>
        /// 队长报点覆盖门限：新报点敌需比 bot 当前目标近此比例以上（0.75=近25%）才覆盖（与 McsTestBrainLayer 滞回同参数）
        /// </summary>
        private const float GOAL_SWITCH_ADVANTAGE = 0.75f;

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
                        BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value,
                        EnableKeepFormation = MiyakoCarryServicePlugin.EnableKeepFormation.Value,
                        FormationMatrix = MiyakoCarryServicePlugin.FormationMatrix.Value,
                        FormationSpacing = MiyakoCarryServicePlugin.FormationSpacing.Value,
                        FormationSequentialFill = MiyakoCarryServicePlugin.FormationSequentialFill.Value,
                        PhrasesSilent = MiyakoCarryServicePlugin.PhrasesSilent.Value,
                        EnableSubtitles = MiyakoCarryServicePlugin.EnableSubtitles.Value,
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

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

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

                if (botOwner.EnemiesController.EnemyInfos.TryGetValue(seenEnemy, out var enemyInfo))
                {
                    // 队长报点弱化：bot 当前目标仍存活且可见可射时，新报点敌须有 25% 距离优势才覆盖（多敌防抖，
                    // 与 McsTestBrainLayer 的 GoalEnemy 切换滞回同参数）；无目标/目标失效时立即接管报点
                    if (ShouldKeepCurrentGoalEnemy(botOwner, enemyInfo))
                    {
                        continue;
                    }

                    enemyInfo.IsVisible = true;
                    botOwner.Memory.GoalEnemy = enemyInfo;
                    enemyInfo.PriorityIndex = 0;
                }
            }
        }

        /// <summary>
        /// 队长报点是否应让位于 bot 当前目标（目标仍有效且报点敌无距离优势）
        /// </summary>
        private bool ShouldKeepCurrentGoalEnemy(BotOwner botOwner, EnemyInfo reportedEnemy)
        {
            var currentGoalEnemy = botOwner.Memory.GoalEnemy;
            if (currentGoalEnemy == null || currentGoalEnemy == reportedEnemy)
            {
                return false;
            }

            var currentPerson = currentGoalEnemy.Person;
            if (currentPerson == null || currentPerson.HealthController == null || !currentPerson.HealthController.IsAlive)
            {
                return false;
            }

            if (!currentGoalEnemy.IsVisible || !currentGoalEnemy.CanShoot)
            {
                return false;
            }

            return reportedEnemy.Distance > currentGoalEnemy.Distance * GOAL_SWITCH_ADVANTAGE;
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