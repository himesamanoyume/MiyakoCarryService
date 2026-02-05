using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 不让组队时的RaidMode强制变为Online
    /// </summary>
    internal sealed class MainMenuControllerClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MainMenuControllerClass), nameof(MainMenuControllerClass.method_52));

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Stfld &&
                    instruction.operand is FieldInfo field &&
                    field.Name == "RaidMode")
                {
                    // NotCheaterPlugin.Logger.LogWarning("跳过 RaidMode 赋值");
                    yield return new CodeInstruction(OpCodes.Nop);
                    continue;
                }

                yield return instruction;
            }
        }
    }
}