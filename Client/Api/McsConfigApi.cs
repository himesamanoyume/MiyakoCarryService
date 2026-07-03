using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;
using UnityEngine;

namespace MiyakoCarryService.Client.Api
{
    public static class McsConfigApi
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="description"></param>
        /// <param name="acceptableValues"></param>
        /// <param name="customAttributes"></param>
        /// <param name="mcsBotPlayerConfig"></param>
        /// <returns></returns>
        public static ConfigEntry<T> Register<T>(
            EConfigType type,
            string key,
            T defaultValue,
            string description = "",
            AcceptableValueBase acceptableValues = null,
            ConfigurationManagerAttributes customAttributes = null,
            McsBotPlayerConfig mcsBotPlayerConfig = null
        )
        {
            return MiyakoCarryServicePlugin.Instance.Register(nameof(type), (int)type, key, defaultValue, description, acceptableValues, customAttributes, mcsBotPlayerConfig);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sectionName"></param>
        /// <param name="order"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="description"></param>
        /// <param name="acceptableValues"></param>
        /// <param name="customAttributes"></param>
        /// <param name="mcsBotPlayerConfig"></param>
        /// <returns></returns>
        public static ConfigEntry<T> Register<T>(
            string sectionName,
            int order,
            string key,
            T defaultValue,
            string description = "",
            AcceptableValueBase acceptableValues = null,
            ConfigurationManagerAttributes customAttributes = null,
            McsBotPlayerConfig mcsBotPlayerConfig = null
        )
        {
            return MiyakoCarryServicePlugin.Instance.Register(sectionName, order, key, defaultValue, description, acceptableValues, customAttributes, mcsBotPlayerConfig);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entry"></param>
        /// <param name="dict"></param>
        /// <param name="xCount"></param>
        public static void CustomDrawer<T>(ConfigEntryBase entry, Dictionary<T, string> dict, int xCount) where T : Enum
        {
            var value = (T)entry.BoxedValue;
            var values = Enum.GetValues(typeof(T));
            var options = new string[values.Length];
            var selectedIndex = 0;

            for (int i = 0; i < values.Length; i++)
            {
                var enumValue = (T)values.GetValue(i);
                options[i] = dict.ContainsKey(enumValue) ? dict[enumValue] : enumValue.ToString();
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