using System;
using System.Linq;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using Comfort.Common;

namespace MiyakoCarryService.Client.Datas
{
    public class ExfilData : TriggerData
    {
        private WeakReference<ExfiltrationPoint> _exfilRef;
        private AIExfiltrationPoint _aiExfiltrationPoint;
        public ExfiltrationPoint ExfiltrationPoint => _exfilRef.TryGetTarget(out var exfil) ? exfil : null;

        public ExfilData(ExfiltrationPoint exfiltrationPoint) : base()
        {
            _exfilRef = new WeakReference<ExfiltrationPoint>(exfiltrationPoint);
            _colliders = exfiltrationPoint.transform.GetComponentsInChildren<Collider>().ToList();
        }

        public override string GetActionName() => ExfiltrationPoint.Settings.Name.McsLocalized();

        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, ExfiltrationPoint.gameObject.transform.position)));
        
        public override bool IsDisabled() => ExfiltrationPoint.Status switch
        {
            EExfiltrationStatus.NotPresent => true,
            _ => false
        };

        public override Vector3 GetPos()
        {
            var aiExfiltrationPoint = FindAiExfiltrationPoint();
            if (aiExfiltrationPoint != null)
            {
                return aiExfiltrationPoint.Position;
            }
            return base.GetPos();
        }

        private AIExfiltrationPoint FindAiExfiltrationPoint()
        {
            if (_aiExfiltrationPoint != null)
            {
                return _aiExfiltrationPoint;
            }

            var exfilPoint = ExfiltrationPoint;
            if (exfilPoint == null || string.IsNullOrEmpty(exfilPoint.Settings.Name))
            {
                return null;
            }

            var aiExfiltrationPoints = Singleton<IBotGame>.Instance?.BotsController?.CoversData?.Patrols?.ExfiltrationPoints;
            if (aiExfiltrationPoints == null)
            {
                return null;
            }

            foreach (var aiExfiltrationPoint in aiExfiltrationPoints)
            {
                if (aiExfiltrationPoint != null && aiExfiltrationPoint.Name == exfilPoint.Settings.Name)
                {
                    _aiExfiltrationPoint = aiExfiltrationPoint;
                    break;
                }
            }
            return _aiExfiltrationPoint;
        }

        public override void Dispose()
        {
            base.Dispose();
            _exfilRef = null;
            _aiExfiltrationPoint = null;
        }
    }
}