using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace MiyakoCarryService.Assistant.Utils
{
    /// <summary>
    /// 为 ConfigurationManager 提供 <c>GUILayout.SelectionGrid</c> 形式的枚举下拉选择器，
    /// 复刻 NotCheater 项目中 <c>EAimPosType</c> 等 CustomDrawer 的注册方式。
    /// </summary>
    internal static class EnumDrawerHelper
    {
        /// <summary>
        /// 绘制枚举选择栅格。<paramref name="localizedNameByValue"/> 缺省的枚举值将回落到 <c>ToString()</c> 文案。
        /// </summary>
        public static void Draw<T>(ConfigEntryBase entry, Dictionary<T, string> localizedNameByValue, int xCount)
            where T : Enum
        {
            var value = (T)entry.BoxedValue;
            var values = Enum.GetValues(typeof(T));
            var options = new string[values.Length];
            var selectedIndex = 0;

            for (int i = 0; i < values.Length; i++)
            {
                var enumValue = (T)values.GetValue(i);
                options[i] = localizedNameByValue != null && localizedNameByValue.ContainsKey(enumValue)
                    ? localizedNameByValue[enumValue]
                    : enumValue.ToString();
                if (enumValue.Equals(value))
                {
                    selectedIndex = i;
                }
            }

            var newIndex = GUILayout.SelectionGrid(selectedIndex, options, xCount);
            if (newIndex != selectedIndex)
            {
                entry.BoxedValue = values.GetValue(newIndex);
            }
        }
    }
}