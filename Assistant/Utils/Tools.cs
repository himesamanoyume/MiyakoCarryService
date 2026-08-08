

using System;
using System.IO;
using System.Linq;
using System.Text;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant.Utils
{
    public static class Tools
    {
        public static float[] Resample(float[] samples, int sourceRate, int targetRate)
        {
            if (samples == null || samples.Length == 0)
            {
                return samples ?? Array.Empty<float>();
            }
            if (sourceRate <= 0 || targetRate <= 0 || sourceRate == targetRate)
            {
                return samples;
            }

            var ratio = (double)targetRate / sourceRate;
            var outLength = (int)Math.Max(1, Math.Round(samples.Length * ratio));
            var output = new float[outLength];
            for (int i = 0; i < outLength; i++)
            {
                var pos = i / ratio;
                var index = (int)pos;
                var frac = (float)(pos - index);
                if (index >= samples.Length - 1)
                {
                    output[i] = samples[samples.Length - 1];
                }
                else
                {
                    output[i] = samples[index] + (samples[index + 1] - samples[index]) * frac;
                }
            }
            return output;
        }

        public static byte[] Encode(float[] samples, int sampleRate = 44100, int channels = 1)
        {
            if (samples == null)
            {
                return Array.Empty<byte>();
            }

            var pcm = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                var v = samples[i];
                v = Math.Max(-1f, Math.Min(1f, v));
                pcm[i] = (short)(v < 0 ? v * 32768f : v * 32767f);
            }

            var byteRate = sampleRate * channels * 2;
            var blockAlign = (short)(channels * 2);
            var dataSize = pcm.Length * 2;
            var totalSize = 44 + dataSize;

            using var memory = new MemoryStream(totalSize);
            using var writer = new BinaryWriter(memory, Encoding.ASCII);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(totalSize - 8);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            var buffer = new byte[pcm.Length * 2];
            Buffer.BlockCopy(pcm, 0, buffer, 0, buffer.Length);
            writer.Write(buffer);

            return memory.ToArray();
        }

        public static string GetLocalizedNames(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                return commandName;
            }
            if (Classification.CommandGlossary.TryGetValue(commandName, out var keys))
            {
                return string.Join(" / ", keys.Select(key => key.McsLocalized()));
            }
            return commandName;
        }

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

            sb.AppendLine(Classification.Terminology);
            sb.AppendLine();

            sb.AppendLine("Command glossary (localized name(s) -> exact CommandName). Map the player's phrase to the EXACT CommandName, never invent a name:");
            foreach (var cmd in Classification.UsableCommands)
            {
                sb.Append("- ").Append(GetLocalizedNames(cmd)).Append(" -> ").AppendLine(cmd);
            }
            sb.AppendLine();

            sb.AppendLine("Proxy commands special handling:");
            sb.AppendLine(" - Prefer the concrete sub-command when the player's intent is specific: QuestProxyAction (quest), LootProxyAction (loot), StationaryWeaponProxyAction (stationary weapon).");
            sb.AppendLine(" - For switch/door and other generic interactable proxying, use InteractionProxyAction; the player must be looking at the target object and the backend resolves it.");
            sb.AppendLine(" - Never emit EndProxyAction — it is a system callback, not a player command.");
            sb.AppendLine();

            sb.AppendLine("Available commands:");
            foreach (var cmd in Classification.UsableCommands)
            {
                sb.Append("- ").Append(cmd).AppendLine();
            }
            sb.AppendLine();

            sb.AppendLine("Target selection grammar (only when player names a specific escort):");
            sb.AppendLine(" - selector = \"All\"  => all alive escorts");
            sb.AppendLine(" - selector = \"ByIndex\", targetIndex = 1..N   => the Nth escort (1-based)");
            sb.AppendLine(" - selector = \"ByCodeName\", targetCodeName = <string>   => escort matching its 代号 (code-name/nickname)");
            sb.AppendLine("Players may name MULTIPLE escorts at once (e.g., \"5号6号\" or \"Rabbit1、Rabbit2\"); then return targetIndices = [5,6] or targetCodeNames = [\"Rabbit1\",\"Rabbit2\"] instead of the single-value fields.");
            sb.AppendLine("Note: \"N号\" is a shorthand for the Nth (1-based) escort; use selector \"ByIndex\" with targetIndices for it.");
            sb.AppendLine("If player did not specify a target, use selector = \"All\".");
            sb.AppendLine("`targetCodeName(s)` are free-form; backend matches against currently alive squad 代号 (codenames).");
            sb.AppendLine();

            sb.AppendLine("`aimingBodyPart` only required when command is \"AimingBodyPart\", one of: " + string.Join(", ", Classification.AimingBodyParts) + ".");
            sb.AppendLine();

            sb.AppendLine("If the phrase cannot be mapped to ANY available command (small talk, filler, acknowledgement, unrelated), return:");
            sb.AppendLine("  {\"error\":\"not_recognized\"}");
            sb.AppendLine("Never output affirmations, acknowledgements, or any natural-language filler. Output only the JSON object for an actual command, or the error object above.");
            sb.AppendLine();
            sb.AppendLine("STT tolerance: the phrase comes from speech-to-text and may contain homophone/transcription errors (e.g., 驻守 \"HoldPosition\" mis-transcribed as 助手 \"assistant\", or 前往 as 犬亡). When the phrase looks like a known command with a homophone or typo, correct it to the CLOSEST command by pronunciation/meaning and map it — do NOT return not_recognized for such cases. Only return not_recognized when the phrase is truly unrelated to any command.");
            sb.AppendLine();

            sb.AppendLine("Command JSON schema:");
            sb.AppendLine("{\"command\":\"<CommandName>\",\"selector\":\"All|ByIndex|ByCodeName|Unspecified\",");
            sb.AppendLine(" \"targetIndices\":[<int>...]|null,\"targetCodeNames\":[<string>...]|null,");
            sb.AppendLine(" \"targetIndex\":<int|null>,\"targetCodeName\":<string|null>,\"aimingBodyPart\":<string|null>,\"optionIndex\":<int|null>}");
            sb.AppendLine("`optionIndex` is ONLY for InteractionProxyAction / QuestProxyAction / StationaryWeaponProxyAction / EscortWorld, referring to the numbered \"Command options\" list appended to the user message (1-based). Set null when the player did not clearly pick one of the listed options.");
            sb.AppendLine("Example: {\"command\":\"GoToPoint\",\"selector\":\"ByIndex\",\"targetIndices\":[5,6],\"targetCodeNames\":null,\"targetIndex\":null,\"targetCodeName\":null,\"aimingBodyPart\":null,\"optionIndex\":null}");
            sb.AppendLine("Example: {\"command\":\"InteractionProxyAction\",\"selector\":\"All\",\"targetIndices\":null,\"targetCodeNames\":null,\"targetIndex\":null,\"targetCodeName\":null,\"aimingBodyPart\":null,\"optionIndex\":3}");
            sb.AppendLine("Example: {\"command\":\"FollowMe\",\"selector\":\"ByIndex\",\"targetIndices\":null,\"targetCodeNames\":null,\"targetIndex\":2,\"targetCodeName\":null,\"aimingBodyPart\":null,\"optionIndex\":null}");

            return sb.ToString();
        }
    }
}