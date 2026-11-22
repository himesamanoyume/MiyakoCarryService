using CommonAssets.Scripts.Cutscenes;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Datas
{
    public class FinalExitZoneData : WorldData
    {
        private readonly FinallExitZone _finalExitZone;
        public FinallExitZone FinalExitZone => _finalExitZone;

        public FinalExitZoneData(FinallExitZone finalExitZone) : base()
        {
            _finalExitZone = finalExitZone;
        }

        public override string GetActionName() => FinallExitZone.FINAL_EXIT_NAME.McsLocalized();

        public override string GetActionTargetName(Vector3 myPlayerPos)
        {
            var distance = FinalExitZone == null ? 0 : Mathf.RoundToInt(Vector3.Distance(myPlayerPos, FinalExitZone.transform.position));
            return string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), distance);
        }

        public override bool IsDisabled()
        {
            return FinalExitZone == null || !FinalExitZone._zoneActive;
        }

        public override Vector3 GetPos()
        {
            if (FinalExitZone == null)
            {
                return Vector3.zero;
            }

            var center = FinalExitZone.transform.position;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                if (NavMesh.SamplePosition(center, out var hit, 1f, -1))
                {
                    return hit.position;
                }
            }

            return center;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
