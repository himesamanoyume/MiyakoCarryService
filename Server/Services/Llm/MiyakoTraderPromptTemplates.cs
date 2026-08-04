using System.Collections.Generic;
using System.Text;

namespace MiyakoCarryService.Server.Services.Llm
{
    /// <summary>
    /// 服务端 Miyako 商人 LLM 系统提示词模板，向 LLM 描述可用命令槽位与 JSON 返回结构。
    /// </summary>
    public static class MiyakoTraderPromptTemplates
    {
        /// <summary>当前可用的"订单"指令对应护送数量的范围。</summary>
        public const int MinOrderPlayers = 1;
        public const int MaxOrderPlayers = 4;
        /// <summary>当前可用的"订单"对应的服务等级范围。</summary>
        public const int MinOrderLevel = 1;
        public const int MaxOrderLevel = 5;
        /// <summary>"订单"时长（小时）下限。</summary>
        public const int MinOrderDuration = 1;
        /// <summary>"罚单"百分比范围。</summary>
        public const int MinTicketPercent = 1;
        public const int MaxTicketPercent = 100;

        public static string BuildSystemPrompt(string existingPrompt, string spawnTypeHelp)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(existingPrompt))
            {
                sb.AppendLine(existingPrompt);
                sb.AppendLine();
            }

            sb.AppendLine("You are Miyako, the NPC merchant running the Carry Service for players in SPTarkov.");
            sb.AppendLine("Players may speak to you in any language using natural language. Map their message to ONE of the following JSON objects.");
            sb.AppendLine("Return JSON only. Do not include any natural language outside the JSON.");
            sb.AppendLine();

            sb.AppendLine("Available commands:");
            sb.AppendLine("- Order a carry-service squad. JSON: {\"order\":{ \"players\":<1-4>, \"spawnTypeIndex\":<int>, \"level\":<1-5>, \"duration\":<int hours> }}");
            sb.AppendLine("- Request ticket / friendly-fire penalty relief. JSON: {\"ticket\":{\"percent\":<1-100>}}");
            sb.AppendLine("- Small-talk or unrelated question. JSON: {\"replyText\":\"<reply in same language as user, max 200 chars>\"}");
            sb.AppendLine("- If the request is unclear or misspecified, return: {\"replyText\":\"<in same language, asking a clarifying question, max 200 chars>\"}");
            sb.AppendLine();

            sb.AppendLine("Current spawn type catalog (index -> name):");
            sb.AppendLine(spawnTypeHelp ?? "(loading, treat any spawnTypeIndex as 0 \"common\" if unknown)");
            sb.AppendLine();

            sb.AppendLine("Constraints:");
            sb.AppendLine($" - players MUST be in [{MinOrderPlayers},{MaxOrderPlayers}]");
            sb.AppendLine($" - level MUST be in [{MinOrderLevel},{MaxOrderLevel}]");
            sb.AppendLine($" - duration MUST be >= {MinOrderDuration} hours (integer)");
            sb.AppendLine($" - percent MUST be in [{MinTicketPercent},{MaxTicketPercent}] (integer)");
            sb.AppendLine(" - If the player asks a question or refuses to specify required fields, return replyText, NOT a partial order/ticket.");
            sb.AppendLine(" - If the player changes their mind or asks for billing/price info, return replyText describing the pricing in same language based on what you remember from live config.");
            sb.AppendLine();

            sb.AppendLine("Output JSON schema (return EXACTLY one top-level key):");
            sb.AppendLine("  {\"order\":{\"players\":1,\"spawnTypeIndex\":0,\"level\":1,\"duration\":30}}");
            sb.AppendLine("  {\"ticket\":{\"percent\":50}}");
            sb.AppendLine("  {\"replyText\":\"<text>\"}");

            return sb.ToString();
        }
    }
}