
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public class ExfilDataMgr : GameWorldDataMgr<ExfilDataMgr>
    {
        protected override void OnRaidStarted()
        {
            base.OnRaidStarted();
            LoadData(LoadExfiltrationPoints);
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
                        var data = exfiltrationPoint.GetData();
                        if (data != null)
                        {
                            _datas.Add(data);
                        }
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
                            var data = scavExfiltrationPoint.GetData();
                            if (data != null)
                            {
                                _datas.Add(data);
                            }
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
                            var data = secretExfiltrationPoint.GetData();
                            if (data != null)
                            {
                                _datas.Add(data);
                            }
                        }
                    }
                }
            });
        }
    }
}