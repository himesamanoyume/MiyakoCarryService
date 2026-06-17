
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class ExfilDataMgr : TriggerDataMgr<ExfilDataMgr>
    {
        protected sealed override void OnRaidStarted()
        {
            LoadData(LoadExfiltrationPoints);
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            OnRaidEnded();
        }

        private void LoadExfiltrationPoints()
        {
            Singleton<GameWorld>.Instance.AllPlayersEverExisted.ExecuteForEach((Player player) =>
            {
                if (!player.IsYourPlayer)
                {
                    return;
                }

                var exfiltrationController = Singleton<GameWorld>.Instance.ExfiltrationController;
                if (player.Profile.Side != EPlayerSide.Savage)
                {
                    exfiltrationController.EligiblePoints(player.Profile).ExecuteForEach((exfiltrationPoint) =>
                    {
                        _datas.Add(exfiltrationPoint.GetData());
                    });
                }
                else
                {
                    var mask = exfiltrationController.GetScavExfiltrationMask(player.Profile.Id);
                    for (int i = 0; i < 31; i++)
                    {
                        if ((mask & (1 << i)) != 0)
                        {
                            var scavExfiltrationPoint = exfiltrationController.ScavExfiltrationPoints[i];
                            _datas.Add(scavExfiltrationPoint.GetData());
                        }
                    }
                }

                var secretExfiltrationPoints = exfiltrationController.SecretEligiblePoints();
                if (secretExfiltrationPoints != null)
                {
                    for (int i = 0; i < secretExfiltrationPoints.Length; i++)
                    {
                        var secretExfiltrationPoint = secretExfiltrationPoints[i];
                        if (secretExfiltrationPoint != null)
                        {
                            _datas.Add(secretExfiltrationPoint.GetData());
                        }
                    }
                }
            });
        }
    }
}