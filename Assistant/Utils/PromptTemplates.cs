using System.Collections.Generic;
using System.Text;
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Assistant.Utils
{
    /// <summary>
    /// LLM 系统提示词模板，向 LLM 描述可用指令槽位、护航选择语法与返回 JSON 结构。
    /// </summary>
    internal static class PromptTemplates
    {
        public static readonly IReadOnlyList<string> UsableCommands =
        [
            // 无需目标的指令
            ECommandType.FollowMe.ToString(),
            ECommandType.HoldPosition.ToString(),
            ECommandType.Regroup.ToString(),
            ECommandType.OnYourOwn.ToString(),
            ECommandType.ChangeFormation.ToString(),
            ECommandType.GoToExfil.ToString(),
            ECommandType.ClearArea.ToString(),
            ECommandType.OpenInventory.ToString(),
            ECommandType.ExcludeOrTakeOver.ToString(),
            ECommandType.ReportAboutEnemy.ToString(),
            ECommandType.ReportAboutSelf.ToString(),
            ECommandType.EndProxyAction.ToString(),
            ECommandType.EscortBtr.ToString(),
            // 需要准星射线/目标的指令：仅由玩家口述动词，Position/TargetId 由 Assistant 后端补全
            ECommandType.GoToPoint.ToString(),
            ECommandType.EscortWorld.ToString(),
            ECommandType.Teleport.ToString(),
            ECommandType.AimingBodyPart.ToString(),
            ECommandType.QuestProxyAction.ToString(),
            ECommandType.LootProxyAction.ToString(),
            ECommandType.InteractionProxyAction.ToString(),
            ECommandType.StationaryWeaponProxyAction.ToString(),
            ECommandType.DropTargetLoot.ToString(),
        ];

        public static readonly IReadOnlyList<string> AimingBodyParts = ["Head", "Body", "LeftArm", "RightArm", "LeftLeg", "RightLeg"];

        public static string BuildSystemPrompt(string existingPrompt)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(existingPrompt))
            {
                sb.AppendLine(existingPrompt);
                sb.AppendLine();
            }

            sb.AppendLine("You are the voice command interpreter for an in-game AI escort squad (MiyakoCarryService).");
            sb.AppendLine("Map the player's spoken phrase to ONE of the following JSON objects.");
            sb.AppendLine("Return JSON only. Do not include any natural language outside the JSON.");
            sb.AppendLine();

            sb.AppendLine("Available commands:");
            foreach (var cmd in UsableCommands)
            {
                sb.Append("- ").Append(cmd).AppendLine();
            }
            sb.AppendLine();

            sb.AppendLine("Target selection grammar (only when player names a specific escort):");
            sb.AppendLine(" - selector = \"All\"  => all alive escorts");
            sb.AppendLine(" - selector = \"ByIndex\", targetIndex = 1..N   => the Nth escort (1-based)");
            sb.AppendLine(" - selector = \"ByCodeName\", targetCodeName = <string>   => escort matching code-name/nickname");
            sb.AppendLine("If player did not specify a target, use selector = \"All\".");
            sb.AppendLine("`targetCodeName` is free-form; backend matches against currently alive squad codenames.");
            sb.AppendLine();

            sb.AppendLine("`aimingBodyPart` only required when command is \"AimingBodyPart\", one of: " +
                          string.Join(", ", AimingBodyParts) + ".");
            sb.AppendLine();

            sb.AppendLine("If the phrase is small-talk or unrelated to escort commands, return:");
            sb.AppendLine("  {\"replyText\": \"<reply in same language as user, max 80 chars>\"}");
            sb.AppendLine();

            sb.AppendLine("Command JSON schema:");
            sb.AppendLine("{\"command\":\"<CommandName>\",\"selector\":\"All|ByIndex|ByCodeName|Unspecified\",");
            sb.AppendLine(" \"targetIndex\":<int|null>,\"targetCodeName\":<string|null>,\"aimingBodyPart\":<string|null>}");
            sb.AppendLine("Example: {\"command\":\"FollowMe\",\"selector\":\"ByIndex\",\"targetIndex\":2,\"targetCodeName\":null,\"aimingBodyPart\":null}");

            return sb.ToString();
        }
    }
}