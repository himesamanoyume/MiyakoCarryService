using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Api
{
    public static class McsConfigApi
    {
        /// <summary>
        /// 
        /// </summary>
        public static ConfigEntry<T> Register<T>(
            EConfigType type,
            string key,
            T defaultValue,
            string description = "",
            AcceptableValueBase acceptableValues = null,
            ConfigurationManagerAttributes customAttributes = null,
            bool needNotify = true,
            bool isHide = false,
            Func<T> getLocal = null,
            Action<T> apply = null
        )
        {
            return Register(nameof(type), (int)type, key, defaultValue, description, acceptableValues, customAttributes, needNotify, isHide, getLocal, apply);
        }

        /// <summary>
        /// 
        /// </summary>
        public static ConfigEntry<T> Register<T>(
            string sectionName,
            int order,
            string key,
            T defaultValue,
            string description = "",
            AcceptableValueBase acceptableValues = null,
            ConfigurationManagerAttributes customAttributes = null,
            bool needNotify = true,
            bool isHide = false,
            Func<T> getLocal = null,
            Action<T> apply = null
        )
        {
            if (EConfigType.BASIC.ToString() == sectionName && (getLocal == null || apply == null))
            {
                throw new Exception("为BASIC分类注册配置项时，必须传递getLocal和apply委托");
            }

            McsBotPlayerConfigUtils.Register(key, getLocal, apply);
            return MiyakoCarryServicePlugin.Instance.Register(sectionName, order, key, defaultValue, description, acceptableValues, customAttributes, needNotify, isHide);
        }

        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<string, McsConfigValue> GetConfigSnapshot()
        {
            return McsBotPlayerConfigUtils.Snapshot();
        }
    }
}