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
        /// <returns></returns>
        public static ConfigEntry<T> Register<T>(
            EConfigType type,
            string key,
            T defaultValue,
            string description = "",
            AcceptableValueBase acceptableValues = null,
            ConfigurationManagerAttributes customAttributes = null,
            bool needNotify = true,
            bool isHide = false
        )
        {
            return MiyakoCarryServicePlugin.Instance.Register(nameof(type), (int)type, key, defaultValue, description, acceptableValues, customAttributes, needNotify, isHide);
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
        /// <returns></returns>
        public static ConfigEntry<T> Register<T>(
            string sectionName,
            int order,
            string key,
            T defaultValue,
            string description = "",
            AcceptableValueBase acceptableValues = null,
            ConfigurationManagerAttributes customAttributes = null,
            bool needNotify = true,
            bool isHide = false
        )
        {
            return MiyakoCarryServicePlugin.Instance.Register(sectionName, order, key, defaultValue, description, acceptableValues, customAttributes, needNotify, isHide);
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
            MiyakoCarryServicePlugin.Instance.CustomDrawer(entry, dict, xCount);
        }
    }
}