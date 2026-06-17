using System.Reflection;
using EFT.UI.Matchmaker;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 为加载地图时的队伍信息列表增加滚动视图，使其能够观察所有战局内的玩家信息
    /// </summary>
    public sealed class PartyInfoPanelScrollPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(PartyInfoPanel), nameof(PartyInfoPanel.Show));

        private static bool _scrollSetupDone = false;

        [PatchPostfix]
        public static void Postfix(PartyInfoPanel __instance, Transform ____playersContainer)
        {
            if (_scrollSetupDone)
            {
                return;
            }

            var panelRect = __instance.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }
            panelRect.anchorMin = new Vector2(0f, 1f);

            var verticalLayoutGroup = __instance.GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup == null)
            {
                return;
            }

            verticalLayoutGroup.enabled = false;

            var containerRect = ____playersContainer.GetComponent<RectTransform>();
            if (containerRect == null)
            {
                return;
            }

            containerRect.pivot = new Vector2(0f, 1f);

            var csf = ____playersContainer.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = ____playersContainer.gameObject.AddComponent<ContentSizeFitter>();
            }
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = __instance.gameObject.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = __instance.gameObject.AddComponent<ScrollRect>();
            }

            scrollRect.content = containerRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            _scrollSetupDone = true;
        }
    }
}