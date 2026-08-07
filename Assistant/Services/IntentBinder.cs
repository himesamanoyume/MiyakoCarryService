using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Assistant.Services
{
    /// <summary>
    /// 把 <see cref="LlmIntent"/> 与本地活护航成员流绑定，组装 <see cref="McsCommandContext"/> 后派发。
    /// <para>
    /// 主机/单机：调用 <see cref="McsCommandApi.Execute"/> 本地执行。副机：用 <see cref="McsEventApi.Notify"/>
    /// 触发 <see cref="CommandMgrHandleFikaEvent"/>，由 Fika Addon 自动转发到主机（与现有菜单流程同一路径）。
    /// </para>
    /// <para>只针对玩家自己的护航队员；需要目标位置的指令由 <see cref="TargetResolver"/> 用准星射线补全。</para>
    /// </summary>
    internal static class IntentBinder
    {
        private static readonly HashSet<string> CommandsNeedPosition = new()
        {
            ECommandType.GoToPoint.ToString(),
            ECommandType.EscortWorld.ToString(),
            ECommandType.Teleport.ToString(),
            ECommandType.ClearArea.ToString(),
        };

        private static readonly HashSet<string> CommandsNeedTargetId = new()
        {
            ECommandType.QuestProxyAction.ToString(),
            ECommandType.LootProxyAction.ToString(),
            ECommandType.InteractionProxyAction.ToString(),
            ECommandType.StationaryWeaponProxyAction.ToString(),
            ECommandType.DropTargetLoot.ToString(),
        };

        /// <summary>支持"指令菜单选项"（optionIndex）派发的命令类型。</summary>
        private static readonly HashSet<string> OptionCommands = new()
        {
            ECommandType.InteractionProxyAction.ToString(),
            ECommandType.QuestProxyAction.ToString(),
            ECommandType.StationaryWeaponProxyAction.ToString(),
            ECommandType.EscortWorld.ToString(),
        };

        /// <summary>
        /// 把意图绑定到 Llm 解析后选中的护航成员，按主机/副机路径派发。
        /// 返回成功派发的护航成员数量。0 表示未派发（可能因为意图为空、护航为空或选择器未匹配）；
        /// -1 表示目标不可用（如代理开门/拾取战利品未处于"选项显示状态"，应提示玩家对准目标）。
        /// </summary>
        public static int BindAndDispatch(LlmIntent intent)
        {
            if (intent == null || intent.IsError || intent.IsReply || string.IsNullOrEmpty(intent.CommandName))
            {
                return 0;
            }

            if (!GameLoop.Instance.IsVaildGameWorld)
            {
                return 0;
            }

            var mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            var aliveMembers = McsCommandApi.GetAliveMembers();
            if (aliveMembers == null || aliveMembers.Length == 0)
            {
                return 0;
            }

            var targets = SelectTargets(intent, aliveMembers);
            if (targets.Length == 0)
            {
                return 0;
            }

            // 仅在需要 Position 的命令时取准星射线结果；代理/护送类优先使用 LLM 选择的菜单选项
            Vector3? position = null;
            string targetId = null;
            if (CommandsNeedPosition.Contains(intent.CommandName) || CommandsNeedTargetId.Contains(intent.CommandName))
            {
                if (intent.OptionIndex.HasValue && OptionCommands.Contains(intent.CommandName))
                {
                    var options = McsCommandApi.GetVoiceMenuOptions();
                    var idx = intent.OptionIndex.Value - 1;
                    if (idx < 0 || idx >= options.Count)
                    {
                        return -1;
                    }
                    position = options[idx].Position;
                    targetId = options[idx].TargetId;
                }
                else if (intent.CommandName == ECommandType.InteractionProxyAction.ToString()
                         || intent.CommandName == ECommandType.LootProxyAction.ToString())
                {
                    // 代理开门/拾取战利品：必须在"选项显示状态"（复刻补丁条件）下才能执行
                    var resolved = TargetResolver.ResolveProxyTarget(mainPlayer, intent.CommandName);
                    if (resolved == null)
                    {
                        return -1;
                    }
                    position = resolved.Position;
                    targetId = resolved.TargetId;
                }
                else
                {
                    var resolved = TargetResolver.ResolveForPlayer(mainPlayer);
                    if (resolved != null)
                    {
                        position = resolved.Position;
                        targetId = resolved.TargetId;
                    }
                }
            }

            var bodyPart = ParseBodyPart(intent.AimingBodyPart);

            var useFikaBrige = MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost;
            int dispatched = 0;
            foreach (var bot in targets)
            {
                var extensions = new Dictionary<string, McsValue>();

                if (useFikaBrige)
                {
                    McsEventApi.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = bot,
                        CommandPacketType = intent.CommandName,
                        Position = position,
                        TargetId = targetId,
                        AimingBodyPartType = bodyPart,
                        ShouldCheckExclude = true,
                        Extensions = extensions,
                    });
                }
                else
                {
                    var ctx = new McsCommandContext
                    {
                        CommandType = intent.CommandName,
                        Position = position,
                        TargetId = targetId,
                        AimingBodyPartType = bodyPart,
                        McsLeadPlayer = mainPlayer,
                        McsBotPlayer = bot,
                        ShouldCheckExclude = true,
                        Extensions = extensions,
                    };
                    McsCommandApi.Execute(ctx, shouldCheckData: true);
                }

                dispatched++;
            }

            return dispatched;
        }

        private static Player[] SelectTargets(LlmIntent intent, Player[] aliveMembers)
        {
            // 多目标：序号数组（1-based，越界跳过、去重），如 "5号6号"
            if (intent.TargetIndices is { Count: > 0 })
            {
                var result = new List<Player>();
                var seen = new HashSet<int>();
                foreach (var idx in intent.TargetIndices)
                {
                    var i = idx - 1;
                    if (i < 0 || i >= aliveMembers.Length || !seen.Add(i))
                    {
                        continue;
                    }
                    result.Add(aliveMembers[i]);
                }
                return result.ToArray();
            }

            // 多目标：代号数组（逐个包含匹配、去重），如 "Rabbit1、Rabbit2"
            if (intent.TargetCodeNames is { Count: > 0 })
            {
                var result = new List<Player>();
                var seen = new HashSet<Player>();
                foreach (var name in intent.TargetCodeNames)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }
                    foreach (var member in aliveMembers)
                    {
                        var nick = member.Profile?.Nickname;
                        if (string.IsNullOrEmpty(nick)
                            || nick.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0
                            || !seen.Add(member))
                        {
                            continue;
                        }
                        result.Add(member);
                        break;
                    }
                }
                return result.ToArray();
            }

            if (intent.Selector == EIntentTargetSelector.All ||
                intent.Selector == EIntentTargetSelector.Unspecified && !intent.TargetIndex.HasValue && string.IsNullOrEmpty(intent.TargetCodeName))
            {
                return aliveMembers;
            }

            if (intent.Selector == EIntentTargetSelector.ByIndex && intent.TargetIndex.HasValue)
            {
                var idx = intent.TargetIndex.Value - 1; // 1-based
                if (idx < 0 || idx >= aliveMembers.Length) { return Array.Empty<Player>(); }
                return new[] { aliveMembers[idx] };
            }

            if (intent.Selector == EIntentTargetSelector.ByCodeName && !string.IsNullOrEmpty(intent.TargetCodeName))
            {
                foreach (var member in aliveMembers)
                {
                    var name = member.Profile?.Nickname;
                    if (string.IsNullOrEmpty(name)) { continue; }
                    if (name.IndexOf(intent.TargetCodeName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return new[] { member };
                    }
                }
                return Array.Empty<Player>();
            }

            // 退化兜底：未指定 → 全员
            return aliveMembers;
        }

        private static BodyPartType ParseBodyPart(string s)
        {
            if (string.IsNullOrEmpty(s)) { return default; }
            return Enum.TryParse<BodyPartType>(s, ignoreCase: true, out var bp) ? bp : default;
        }
    }
}