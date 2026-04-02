using System;
using System.Reflection;
using ConfigurationManager;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.BepInEx
{
    public sealed class ContainsSearchStringPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ConfigurationManager.ConfigurationManager), "ContainsSearchString");

        [PatchPrefix]
        public static bool Prefix(SettingEntryBase setting, string[] searchStrings, ref bool __result)
        {
            var combinedSearchTarget = setting.PluginInfo.Name + "\n" +
                                        setting.PluginInfo.GUID + "\n" +
                                        setting.DispName.McsLocalized() + "\n" +
                                        setting.Category.McsLocalized() + "\n" +
                                        setting.Description + "\n" +
                                        setting.DefaultValue + "\n" +
                                        setting.Get();

            __result = true;
            foreach (string s in searchStrings)
            {
                if (combinedSearchTarget.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) < 0)
                {
                    __result = false;
                    break;
                }
            }
            return false;
        }
    }
}
