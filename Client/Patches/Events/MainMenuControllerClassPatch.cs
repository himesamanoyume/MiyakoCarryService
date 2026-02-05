// using System.Collections.Generic;
// using System.Reflection;
// using System.Reflection.Emit;
// using HarmonyLib;
// using SPT.Reflection.Patching;

// namespace MiyakoCarryService.Client.Patches.Events
// {
//     /// <summary>
//     /// 不让Scav覆盖掉Pmc数据
//     /// </summary>
//     // internal sealed class MainMenuControllerClass1Patch : ModulePatch
//     // {
//     //     protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MainMenuControllerClass.Struct444), nameof(MainMenuControllerClass.Struct444.MoveNext));

//     //     [PatchTranspiler]
//     //     public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//     //     {
//     //         foreach (var instruction in instructions)
//     //         {
//     //             if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
//     //                 instruction.operand is MethodInfo method &&
//     //                 method.Name == "Apply")
//     //             {
//     //                 MiyakoCarryServicePlugin.Logger.LogError("跳过 Apply 调用");
//     //                 yield return new CodeInstruction(OpCodes.Nop);
//     //                 continue;
//     //             }

//     //             yield return instruction;
//     //         }
//     //     }
//     // }

//     // internal sealed class MainMenuControllerClass2Patch : ModulePatch
//     // {
//     //     protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MainMenuControllerClass), nameof(MainMenuControllerClass.method_52));

//     //     [PatchTranspiler]
//     //     public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//     //     {
//     //         foreach (var instruction in instructions)
//     //         {
//     //             if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
//     //                 instruction.operand is MethodInfo method &&
//     //                 method.Name == "set_RaidMode")
//     //             {
//     //                 MiyakoCarryServicePlugin.Logger.LogError("跳过 RaidMode 赋值Online");
//     //                 yield return new CodeInstruction(OpCodes.Nop);
//     //             }

//     //             yield return instruction;
//     //         }
//     //     }
//     // }
// }