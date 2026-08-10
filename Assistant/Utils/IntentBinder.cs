using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Models;
using UnityEngine;

namespace MiyakoCarryService.Assistant.Utils
{
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

        private static readonly HashSet<string> OptionCommands = new()
        {
            ECommandType.InteractionProxyAction.ToString(),
            ECommandType.QuestProxyAction.ToString(),
            ECommandType.StationaryWeaponProxyAction.ToString(),
            ECommandType.EscortWorld.ToString(),
        };

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
                else if (intent.CommandName == ECommandType.InteractionProxyAction.ToString() || intent.CommandName == ECommandType.LootProxyAction.ToString())
                {
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

            var useFikaBrige = MiyakoCarryServicePlugin.FikaInstalled && !Client.Utils.Tools.IsHost;
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
            if (intent.TargetIndices is { Count: > 0 })
            {
                var result = new List<Player>();
                var seen = new HashSet<Player>();
                foreach (var requested in intent.TargetIndices)
                {
                    foreach (var member in aliveMembers)
                    {
                        if (McsCommandApi.GetMcsBotPlayerIndex(member) != requested || !seen.Add(member))
                        {
                            continue;
                        }
                        result.Add(member);
                        break;
                    }
                }
                return result.ToArray();
            }

            if (intent.TargetCodeNames.Count > 0)
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
                        var nickname = member.Profile?.Nickname;
                        if (string.IsNullOrEmpty(nickname) || nickname.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0 || !seen.Add(member))
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
                var requested = intent.TargetIndex.Value;
                foreach (var member in aliveMembers)
                {
                    if (McsCommandApi.GetMcsBotPlayerIndex(member) == requested)
                    {
                        return [member];
                    }
                }
                return Array.Empty<Player>();
            }

            if (intent.Selector == EIntentTargetSelector.ByName && !string.IsNullOrEmpty(intent.TargetCodeName))
            {
                foreach (var member in aliveMembers)
                {
                    var name = member.Profile?.Nickname;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }
                    if (name.IndexOf(intent.TargetCodeName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return [member];
                    }
                }
                return Array.Empty<Player>();
            }

            return aliveMembers;
        }

        private static BodyPartType ParseBodyPart(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return default;
            }
            return Enum.TryParse<BodyPartType>(s, ignoreCase: true, out var bp) ? bp : default;
        }
    }
}