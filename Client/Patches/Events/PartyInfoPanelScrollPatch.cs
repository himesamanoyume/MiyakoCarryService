using System.Reflection;
using System.Threading.Tasks;
using EFT.UI.Matchmaker;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions.ToIl2Cpp;
using SPTushonka.Reflection.Patching;
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

        [PatchPostfix]
        public static void Postfix(PartyInfoPanel __instance)
        {
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

            var containerRect = __instance._playersContainer.GetComponent<RectTransform>();
            if (containerRect == null)
            {
                return;
            }

            containerRect.pivot = new Vector2(0f, 1f);

            var csf = __instance._playersContainer.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = __instance._playersContainer.gameObject.AddComponent<ContentSizeFitter>();
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
            SetIndex(__instance._separator).McsHandleExceptions();
        }

        private static async Task SetIndex(GameObject separator)
        {
            // 续体拉回主线程：SetSiblingIndex 是纯 Unity API，在线程池上调用会 AV
            await Task.Delay(1000);
            separator.transform.SetSiblingIndex(2);
        }
    }
}
