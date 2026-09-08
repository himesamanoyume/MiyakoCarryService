
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
        /// 队长报点覆盖门限：新报点敌需比 bot 当前目标近此比例以上（0.75=近25%）才覆盖（与 McsBrainLayer 滞回同参数）
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

        /// <summary>
        /// 老板威胁窗口（秒）：窗口内攻击过老板的敌视为高威胁，护航全员强制接管（PitFireTeam PlayerEngagementWindow 同款）
        /// </summary>
        public const float LEAD_THREAT_WINDOW = 3f;

        /// <summary>
        /// 最近一次攻击老板的敌人（AI 或玩家攻击者，GameLoop 老板 BeingHitAction 记录）
        /// </summary>
        public Player LastLeadAttackerPlayer = null;

        /// <summary>
        /// 最近一次攻击老板的时间（Time.time），-999f 表示从未发生
        /// </summary>
        public float LastLeadAttackerTime = -999f;

        /// <summary>
        /// 正瞄威胁窗口（秒）：正瞄老板的敌（其 GoalEnemy 为老板本人且可见/可射/正在射击）
        /// 在此时间内与攻击者同级视为高威胁（扫描周期 1s，窗口略长于周期防止漏帧过期）
        /// </summary>
        public const float LEAD_AIMING_WINDOW = 2f;

        /// <summary>
        /// 最近一次正瞄老板的敌人（PlayerDataMgr 每秒轮询扫描，取距老板最近）
        /// </summary>
        public Player LastLeadAimingEnemyPlayer = null;

        /// <summary>
        /// 最近一次正瞄老板的时间（Time.time），-999f 表示从未扫描到
        /// </summary>
        public float LastLeadAimingTime = -999f;

        /// <summary>
        /// 老板视野内的威胁敌列表（PlayerDataMgr 每秒轮询刷新，按距老板升序；对敌优先级扩展的中威胁来源）
        /// </summary>
        public List<Player> LeadVisibleEnemies = new();

        /// <summary>
        /// 是否为威胁窗口内的高威胁敌（攻击过老板 或 正瞄老板，按 ProfileId 比对，兼容 IPlayer/Player 混用场景）
        /// </summary>
        public bool IsLeadThreatEnemy(IPlayer enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            var time = Time.time;
            if (LastLeadAttackerPlayer != null
                && enemy.ProfileId == LastLeadAttackerPlayer.ProfileId
                && time - LastLeadAttackerTime <= LEAD_THREAT_WINDOW)
            {
                return true;
            }

            if (LastLeadAimingEnemyPlayer != null
                && enemy.ProfileId == LastLeadAimingEnemyPlayer.ProfileId
                && time - LastLeadAimingTime <= LEAD_AIMING_WINDOW)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 当前有效的高威胁敌仲裁：攻击过老板的敌优先，其次正瞄老板的敌（含存活与窗口校验，均无效返回 null）
        /// </summary>
        public Player GetLeadThreatEnemy()
        {
            if (LastLeadAttackerPlayer != null
                && LastLeadAttackerPlayer.HealthController != null
                && LastLeadAttackerPlayer.HealthController.IsAlive
                && Time.time - LastLeadAttackerTime <= LEAD_THREAT_WINDOW)
            {
                return LastLeadAttackerPlayer;
            }

            if (LastLeadAimingEnemyPlayer != null
                && LastLeadAimingEnemyPlayer.HealthController != null
                && LastLeadAimingEnemyPlayer.HealthController.IsAlive
                && Time.time - LastLeadAimingTime <= LEAD_AIMING_WINDOW)
            {
                return LastLeadAimingEnemyPlayer;
            }

            return null;
        }

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        public void CleanupDeadEnemies()
        {
            // 威胁上下文清理：死亡敌不再作为高威胁/中威胁来源（LeadVisibleEnemies 每秒全量刷新，此处仅即时剔除）
            if (LastLeadAttackerPlayer != null && !LastLeadAttackerPlayer.HealthController.IsAlive)
            {
                LastLeadAttackerPlayer = null;
            }

            if (LastLeadAimingEnemyPlayer != null && !LastLeadAimingEnemyPlayer.HealthController.IsAlive)
            {
                LastLeadAimingEnemyPlayer = null;
            }

            LeadVisibleEnemies.RemoveAll(enemy => enemy == null || enemy.HealthController == null || !enemy.HealthController.IsAlive);

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

        /// <summary>
        /// 老板被打报点节流间隔（秒）：连发武器连续命中时避免高频全队遍历（威胁上下文记录不受节流影响）
        /// </summary>
        public const float LEAD_HIT_REPORT_INTERVAL = 0.25f;

        /// <summary>
        /// 最近一次老板被打报点的时间（Time.time），-999f 表示从未报点
        /// </summary>
        public float LastLeadReportTime = -999f;

        /// <summary>
        /// 记录老板威胁上下文（GameLoop 老板 BeingHitAction 调用，AI 与玩家攻击者均记录）：
        /// 威胁窗口内（LEAD_THREAT_WINDOW）该敌被视为高威胁，护航全员强制接管
        /// </summary>
        public void MarkLeadAttacker(Player attacker)
        {
            if (attacker == null)
            {
                return;
            }

            LastLeadAttackerPlayer = attacker;
            LastLeadAttackerTime = Time.time;
        }

        /// <summary>
        /// 记录正瞄威胁上下文（PlayerDataMgr 每秒扫描调用）：正瞄老板的敌（其 GoalEnemy 为老板本人）
        /// 在 LEAD_AIMING_WINDOW 内与攻击者同级视为高威胁；攻击者状态位不受影响（直接威胁优先）
        /// </summary>
        public void MarkLeadAimingEnemy(Player aimingEnemy)
        {
            if (aimingEnemy == null)
            {
                return;
            }

            LastLeadAimingEnemyPlayer = aimingEnemy;
            LastLeadAimingTime = Time.time;
        }

        /// <summary>
        /// 老板开火报点节流间隔（秒）：全自动连发时每发都做弹道推断无意义（PitFireTeam 同款参数）
        /// </summary>
        public const float LEAD_SHOT_REPORT_INTERVAL = 0.15f;

        /// <summary>
        /// 最近一次老板开火报点的时间（Time.time），-999f 表示从未报点
        /// </summary>
        public float LastLeadShotReportTime = -999f;

        public void CalcGoalEnemy(Player seenEnemy)
        {
            CleanupDeadEnemies();

            if (seenEnemy == null || !seenEnemy.HealthController.IsAlive)
            {
                return;
            }

            var mcsBotPlayers = McsMgr.GetAllMcsSquadMembersByMcsLeadId(McsLeadPlayer.ProfileId);
            var isLeadThreatEnemy = IsLeadThreatEnemy(seenEnemy);

            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                var botOwner = mcsBotPlayer.BotOwner;

                // 报点入组统一用 Player 本体做键：原生自然目击流程按 Player 键入 EnemyInfos，
                // BotOwner 与 Player 互不等价（无 Equals 重写），混用会造成同敌双份记录且 TryGetValue 永远失配
                McsLeadPlayer.BotsGroup.AddEnemy(seenEnemy, EBotEnemyCause.callForHelp2);

                if (!botOwner.EnemiesController.EnemyInfos.TryGetValue(seenEnemy, out var enemyInfo))
                {
                    // 注入补全：成员尚不认识该敌时手动建 EnemyInfo（BotEnemiesController.AddNew+SetInfo 原生路径，
                    // 兜底组级 AddEnemy 被拦截的场景如 Zryachiy 系护航/醉酒玩家限制）
                    if (!TryRegisterEnemyForFollower(botOwner, seenEnemy, out enemyInfo))
                    {
                        continue;
                    }
                }

                // 队长报点弱化：bot 当前目标仍存活且可见可射时，新报点敌须有 25% 距离优势才覆盖（多敌防抖，
                // 与 McsBrainLayer 的 GoalEnemy 切换滞回同参数）；无目标/目标失效时立即接管报点；
                // 高威胁敌（威胁窗口内攻击过老板）豁免弱化，全员强制接管
                if (!isLeadThreatEnemy && ShouldKeepCurrentGoalEnemy(botOwner, enemyInfo))
                {
                    continue;
                }

                enemyInfo.IsVisible = true;
                botOwner.Memory.GoalEnemy = enemyInfo;
                enemyInfo.PriorityIndex = 0;
            }
        }

        /// <summary>
        /// 报点敌注入补全：成员尚不认识该敌时手动建 EnemyInfo 并入 per-bot 敌表（原生 BotEnemiesController.AddNew + SetInfo 路径），
        /// 使报点敌可被 GoalEnemy 直接持有；组级敌情信息优先复用老板组的（含 cause 来源），缺失时构造最小信息。
        /// 注入后伪造个人接触记录（像"刚刚亲眼见过"），IsVisible 按 LOS 实测设置——
        /// 有视线立即开火响应，无视线保持不可见（bot 转向最后已知点/压制射击，避免对墙盲射）
        /// </summary>
        private bool TryRegisterEnemyForFollower(BotOwner botOwner, Player seenEnemy, out EnemyInfo enemyInfo)
        {
            enemyInfo = null;

            if (botOwner?.EnemiesController == null || botOwner.BotsGroup == null)
            {
                return false;
            }

            if (!McsLeadPlayer.BotsGroup.Enemies.TryGetValue(seenEnemy, out var groupInfo))
            {
                groupInfo = new BotGroupEnemyInfo(seenEnemy, McsLeadPlayer.BotsGroup, EBotEnemyCause.callForHelp2);
            }

            enemyInfo = botOwner.EnemiesController.AddNew(botOwner.BotsGroup, seenEnemy, groupInfo);
            botOwner.EnemiesController.SetInfo(seenEnemy, enemyInfo);

            // 伪造个人接触记录：记忆状态完整（HaveSeenPersonal/见过时间/最后位置），
            // 下游 IsEnemyPosLost/压制射击等依赖 LastSeenTime 的行为按"刚见过"处理
            var time = Time.time;
            enemyInfo.HaveSeenPersonal = true;
            enemyInfo.PersonalSeenTime = time;
            enemyInfo.PersonalLastSeenTime = time;
            enemyInfo.PersonalLastPos = seenEnemy.Position;
            if (enemyInfo.FirstTimeSeen < 0f)
            {
                enemyInfo.FirstTimeSeen = time;
            }

            // LOS 实测：bot 眼位到敌无遮挡才置可见（可立即开火），否则保持不可见（记忆驱动逼近）
            var botEyePos = botOwner.Position + Vector3.up * 1.6f;
            var blocked = Physics.Linecast(
                botEyePos,
                seenEnemy.Position + Vector3.up * 1.6f,
                out _,
                LayersMaskController.HighPolyWithTerrainMask
            );
            enemyInfo.SetVisible(!blocked);
            return true;
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