using System.Reflection;
using ConfigurationManager;
using SPT.Reflection.Patching;
using UnityEngine;
using HarmonyLib;
using BepInEx;
using System.Collections.Generic;
using MiyakoCarryService.Client.Utils;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Patches.BepInEx
{
    public sealed class DrawSinglePluginPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ConfigurationManager.ConfigurationManager), "DrawSinglePlugin");
        private static Dictionary<string, List<SettingEntryBase>> _allSettings = new();

        private static FormationDataMgr FormationDataMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<FormationDataMgr>();
            }
        }

        [PatchPrefix]
        public static bool Prefix(ConfigurationManager.ConfigurationManager __instance, object plugin)
        {
            var pluginTraverse = Traverse.Create(plugin);

            var pluginInfo = pluginTraverse.Field<BepInPlugin>("Info").Value;
            if (pluginInfo == null)
            {
                return true;
            }

            var guid = pluginInfo.GUID;
            if (guid == MiyakoCarryServicePlugin.McsGUID)
            {
                DrawSinglePluginCustom(__instance, plugin, pluginTraverse);
                return false;
            }

            return true;
        }

        private static Traverse _instanceTraverse = null;

        private static void DrawSinglePluginCustom(ConfigurationManager.ConfigurationManager __instance, object plugin, Traverse pluginTraverse)
        {
            if (_instanceTraverse == null)
            {
                _instanceTraverse = Traverse.Create(__instance);
            }

            var cachedHeight = pluginTraverse.Field<int>("Height").Value;
            var startRect = new Rect();
            if (Event.current.type == EventType.Repaint)
            {
                startRect = GUILayoutUtility.GetLastRect();
            }

            var pluginInfo = pluginTraverse.Field<BepInPlugin>("Info").Value;
            var pluginCollapsed = pluginTraverse.Property<bool>("Collapsed").Value;
            var pluginCategories = pluginTraverse.Field<object>("Categories").Value;
            var pluginWebsite = pluginTraverse.Field<string>("Website").Value;

            var showDebug = _instanceTraverse.Field<bool>("_showDebug").Value;
            var searchString = _instanceTraverse.Property<string>("SearchString").Value;
            var hideSingleSectionValue = _instanceTraverse.Field<object>("_hideSingleSection").Value;
            var tipsPluginHeaderWasClicked = _instanceTraverse.Field<bool>("_tipsPluginHeaderWasClicked");

            var _fieldDrawer = _instanceTraverse.Field<object>("_fieldDrawer").Value;
            var _advancedSettingColor = _instanceTraverse.Field<Color>("_advancedSettingColor").Value;
            var _leftColumnWidth = _instanceTraverse.Property<int>("LeftColumnWidth").Value;
            var _rightColumnWidth = _instanceTraverse.Property<int>("RightColumnWidth").Value;

            GUILayout.BeginVertical(GUI.skin.box);

            GUIContent categoryHeader;
            if (pluginInfo != null)
            {
                categoryHeader = showDebug ?
                    new GUIContent($"{pluginInfo.Name.TrimStart('!')} {pluginInfo.Version}", null, "GUID: " + pluginInfo.GUID) :
                    new GUIContent($"{pluginInfo.Name.TrimStart('!')} {pluginInfo.Version}");
            }
            else
            {
                categoryHeader = new GUIContent("Unknown Plugin");
            }

            var isSearching = !string.IsNullOrEmpty(searchString);

            {
                var hasWebsite = !string.IsNullOrEmpty(pluginWebsite);
                if (hasWebsite)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(29);
                }

                var drawPluginHeaderMethod = AccessTools.Method(Assembly.GetAssembly(__instance.GetType()).GetType("ConfigurationManager.SettingFieldDrawer"), "DrawPluginHeader", [typeof(GUIContent), typeof(bool)]);
                bool headerClicked = (bool)drawPluginHeaderMethod?.Invoke(null, [categoryHeader, pluginCollapsed && !isSearching]);

                if (headerClicked && !isSearching)
                {
                    tipsPluginHeaderWasClicked.Value = true;
                    // 切换Collapsed状态
                    pluginTraverse.Property("Collapsed").SetValue(!pluginCollapsed);
                }

                if (hasWebsite)
                {
                    var origColor = GUI.color;
                    GUI.color = Draw.Gray.Rgb;
                    if (GUILayout.Button(new GUIContent(Locales.ORIGINALWEBSITE.McsLocalized(), null, pluginWebsite), GUI.skin.label, GUILayout.ExpandWidth(false)))
                    {
                        var utilsType = typeof(ConfigurationManager.ConfigurationManager).Assembly.GetType("ConfigurationManager.Utilities.Utils");
                        var openWebsiteMethod = AccessTools.Method(utilsType, "OpenWebsite", [typeof(string)]);
                        openWebsiteMethod.Invoke(null, [pluginWebsite]);
                    }
                    GUI.color = origColor;
                    GUILayout.EndHorizontal();
                }
            }

            if (isSearching || !pluginCollapsed)
            {
                if (Locales.BASIC.McsLocalized().Contains("Mcs"))
                {
                    CustomDrawCategoryHeader(Locales.LOADINGLOCALES.McsLocalized());
                }

                if (pluginCategories != null)
                {
                    var categories = pluginCategories as System.Collections.IList;
                    if (categories != null)
                    {
                        _allSettings.Clear();
                        foreach (var category in categories)
                        {
                            var categoryTraverse = Traverse.Create(category);
                            var categoryName = categoryTraverse.Field<string>("Name").Value;
                            var categorySettings = categoryTraverse.Field<List<SettingEntryBase>>("Settings").Value;
                            _allSettings.Add(categoryName, categorySettings);
                        }

                        var shouldDrawHeader = categories.Count > 1;

                        foreach (var item in _allSettings)
                        {
                            CustomDrawCategory(item.Key, shouldDrawHeader, item.Value, hideSingleSectionValue, _fieldDrawer, _advancedSettingColor, _leftColumnWidth);
                        }
                    }
                }
            }

            GUILayout.EndVertical();

            // 计算并更新高度
            if (Event.current.type == EventType.Repaint)
            {
                Rect endRect = GUILayoutUtility.GetLastRect();
                var actualHeight = (int)(endRect.y - startRect.y + endRect.height);

                // 只有高度变化时才更新
                if (actualHeight != cachedHeight)
                {
                    pluginTraverse.Field("Height").SetValue(actualHeight);
                    // 强制重绘以更新滚动区域
                    GUI.changed = true;
                }
            }
        }

        // 用于存储每个Category的折叠状态
        private static Dictionary<string, bool> _categoryCollapseStates = new Dictionary<string, bool>();
        private static Dictionary<string, bool> _formationCollapseStates = new Dictionary<string, bool>();

        private static void CustomDrawCategory(string categoryName, bool shouldDrawHeader, List<SettingEntryBase> categorySettings, object hideSingleSectionValue, object fieldDrawer, Color advancedSettingColor, int leftColumnWidth)
        {
            var categoryLocalizedName = categoryName.McsLocalized();
            if (!string.IsNullOrEmpty(categoryName))
            {
                if (hideSingleSectionValue != null)
                {
                    var hideSingleSectionTraverse = Traverse.Create(hideSingleSectionValue);
                    var hideSingleSection = hideSingleSectionTraverse.Property<bool>("Value").Value;
                    shouldDrawHeader = shouldDrawHeader || !hideSingleSection;
                }

                if (shouldDrawHeader)
                {
                    // 获取或初始化折叠状态
                    if (!_categoryCollapseStates.ContainsKey(categoryName))
                    {
                        _categoryCollapseStates[categoryName] = true; // 默认折叠
                    }

                    var isCategoryCollapsed = _categoryCollapseStates[categoryName];

                    // 绘制可点击的Category头部
                    if (CustomDrawCategoryHeader(categoryLocalizedName, isCategoryCollapsed))
                    {
                        // 切换折叠状态
                        _categoryCollapseStates[categoryName] = !isCategoryCollapsed;
                        isCategoryCollapsed = _categoryCollapseStates[categoryName];
                    }

                    // 只有在未折叠状态下才绘制设置项
                    if (isCategoryCollapsed)
                    {
                        return;
                    }

                    if (categorySettings == null)
                    {
                        return;
                    }

                    foreach (var setting in categorySettings)
                    {
                        CustomDrawSingleSetting(setting, fieldDrawer, advancedSettingColor, leftColumnWidth);
                        GUILayout.Space(2);

                        if (FormationDataMgr == null)
                        {
                            continue;
                        }

                        if (setting.DispName == "保存队形预设快捷键")
                        {
                            var formationDatas = FormationDataMgr.GetDatas<FormationData>();
                            foreach (var formationData in formationDatas)
                            {
                                CustomDrawFormationSingleSetting(formationData, leftColumnWidth);
                            }
                        }
                    }
                }
                else
                {
                    // 如果不需要绘制头部，直接显示设置项
                    if (categorySettings == null)
                    {
                        return;
                    }

                    foreach (var setting in categorySettings)
                    {
                        CustomDrawSingleSetting(setting, fieldDrawer, advancedSettingColor, leftColumnWidth);
                        GUILayout.Space(2);

                        if (setting.DispName == "保存队形预设快捷键")
                        {
                            var formationDatas = FormationDataMgr.GetDatas<FormationData>();
                            foreach (var formationData in formationDatas)
                            {
                                CustomDrawFormationSingleSetting(formationData, leftColumnWidth);
                            }
                        }
                    }
                }
            }
        }

        private static bool CustomDrawCategoryHeader(string categoryLocalizedName, bool isCollapsed)
        {
            var categoryHeaderStyle = CustomDrawCategoryHeaderComponent();

            categoryHeaderStyle.normal.background = null;
            categoryHeaderStyle.hover.background = null;
            categoryHeaderStyle.active.background = null;

            var content = new GUIContent(categoryLocalizedName);
            if (isCollapsed)
            {
                content.text += "\n...";
            }

            return GUILayout.Button(content, categoryHeaderStyle, GUILayout.ExpandWidth(true));
        }

        private static GUIStyle CustomDrawCategoryHeaderComponent()
        {
            var categoryHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                stretchWidth = true,
                fontSize = 14
            };
            return categoryHeaderStyle;
        }

        private static void CustomDrawCategoryHeader(string categoryName)
        {
            var categoryHeaderStyle = CustomDrawCategoryHeaderComponent();
            GUILayout.Label(categoryName, categoryHeaderStyle);
        }

        private static void CustomDrawSingleSetting(SettingEntryBase setting, object fieldDrawer, Color advancedSettingColor, int leftColumnWidth)
        {
            if (MiyakoCarryServicePlugin.HideList.Contains(setting.DispName))
            {
                return;
            }

            GUILayout.BeginHorizontal();
            {
                CustomDrawSettingName(setting, advancedSettingColor, leftColumnWidth);
                var fieldDrawerTraverse = Traverse.Create(fieldDrawer);
                fieldDrawerTraverse.Method("DrawSettingValue", [typeof(SettingEntryBase)]).GetValue(setting);
                AccessTools.Method(typeof(ConfigurationManager.ConfigurationManager), "DrawDefaultButton", [typeof(SettingEntryBase)]).Invoke(null, [setting]);
            }
            GUILayout.EndHorizontal();
        }

        private static void CustomDrawSettingName(SettingEntryBase setting, Color advancedSettingColor, int leftColumnWidth)
        {
            if (setting.HideSettingName)
            {
                return;
            }

            var origColor = GUI.color;
            if (setting.IsAdvanced == true)
            {
                GUI.color = advancedSettingColor;
            }

            GUILayout.Label(new GUIContent(setting.DispName.TrimStart('!').McsLocalized(), null, setting.Description.McsLocalized()), GUILayout.Width(leftColumnWidth), GUILayout.MaxWidth(leftColumnWidth));
            GUI.color = origColor;
        }

        private static void CustomDrawFormationSingleSetting(FormationData formationData, int leftColumnWidth)
        {
            if (!_formationCollapseStates.ContainsKey(formationData.Name))
            {
                _formationCollapseStates[formationData.Name] = true;
            }

            var isFormationCollapsed = _formationCollapseStates[formationData.Name];

            if (CustomDrawCategoryHeader(formationData.Name, isFormationCollapsed))
            {
                _formationCollapseStates[formationData.Name] = !isFormationCollapsed;
                isFormationCollapsed = _formationCollapseStates[formationData.Name];
            }

            if (isFormationCollapsed)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            var newName = GUILayout.TextField(formationData.Name, GUILayout.Width(leftColumnWidth), GUILayout.MaxWidth(leftColumnWidth));
            var newFormationMatrix = Tools.DrawFormationMatrix(formationData.Name, formationData.FormationMatrix);
            if (newName != formationData.Name || newFormationMatrix != formationData.FormationMatrix)
            {
                Tools.RemoveFormationOpenCell(formationData.Name);
                FormationDataMgr.SaveFormationPreset(formationData.Id, newName, newFormationMatrix);
                _formationCollapseStates[newName] = false;
            }
            if (GUILayout.Button(Locales.DELETEFORMATION.McsLocalized(), GUILayout.ExpandWidth(false)))
            {
                Tools.RemoveFormationOpenCell(formationData.Name);
                _formationCollapseStates.Remove(formationData.Name);
                FormationDataMgr.DeleteFormation(formationData);
            }
            GUILayout.EndHorizontal();
        }
    }
}
