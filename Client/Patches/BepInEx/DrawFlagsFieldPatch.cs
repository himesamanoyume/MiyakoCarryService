using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using SPT.Reflection.Patching;
using System.Reflection;
using ConfigurationManager;
using UnityEngine;
using HarmonyLib;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Patches.BepInEx
{

    internal sealed class DrawFlagsFieldPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ConfigurationManager.ConfigurationManager).Assembly.GetType("ConfigurationManager.SettingFieldDrawer"), "DrawFlagsField");

        [PatchPrefix]
        public static bool Prefix(SettingEntryBase setting, IList enumValues, int maxWidth)
        {
            if (setting.SettingType == typeof(EBlockItemType))
            {
                var currentValue = Convert.ToInt64(setting.Get());
                var list = new List<(string name, long val)>();

                foreach (object obj in enumValues)
                {
                    if (obj is Enum e)
                    {
                        var field = e.GetType().GetField(e.ToString());
                        var descAttr = field?.GetCustomAttribute<DescriptionAttribute>();
                        if (descAttr != null)
                        {
                            list.Add((
                                name: descAttr.Description.McsLocalized(),
                                val: Convert.ToInt64(e)
                            ));
                        }
                    }
                }
                var allValues = list.ToArray();

                GUILayout.BeginVertical(GUILayout.MaxWidth(maxWidth));
                {
                    for (var index = 0; index < allValues.Length;)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            var currentWidth = 0;
                            for (; index < allValues.Length; index++)
                            {
                                var value = allValues[index];
                                if (value.val != 0)
                                {
                                    var textDimension = (int)GUI.skin.toggle.CalcSize(new GUIContent(value.name)).x;
                                    currentWidth += textDimension;
                                    if (currentWidth > maxWidth)
                                    {
                                        break;
                                    }

                                    GUI.changed = false;
                                    var newVal = GUILayout.Toggle((currentValue & value.val) == value.val, value.name, GUILayout.ExpandWidth(false));
                                    if (GUI.changed)
                                    {
                                        var newValue = newVal ? currentValue | value.val : currentValue & ~value.val;
                                        setting.Set(Enum.ToObject(setting.SettingType, newValue));
                                    }
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUI.changed = false;
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
