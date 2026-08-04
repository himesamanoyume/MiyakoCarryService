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
        /// 注册配置项
        /// </summary>
        public static ConfigEntry<T> RegisterConfig<T>(
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
            return RegisterConfig(nameof(type), (int)type, key, defaultValue, description, acceptableValues, customAttributes, needNotify, isHide, getLocal, apply);
        }

        /// <summary>
        /// 注册配置项
        /// </summary>
        public static ConfigEntry<T> RegisterConfig<T>(
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
        /// 获取配置项扩展数据
        /// </summary>
        public static Dictionary<string, McsValue> GetConfigSnapshot()
        {
            return McsBotPlayerConfigUtils.Snapshot();
        }
        
        /// <summary>
        /// 自定义枚举配置项绘制
        /// </summary>
        public static void CustomDrawer<T>(ConfigEntryBase entry, Dictionary<T, string> dict, int xCount) where T : Enum
        {
            MiyakoCarryServicePlugin.Instance.CustomDrawer(entry, dict, xCount);
        }
    }
}