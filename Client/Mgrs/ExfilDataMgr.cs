
using System;
using System.Collections.Generic;
using Comfort.Common;
using CommonAssets.Scripts.Cutscenes;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using Il2CppInterop.Runtime.Injection;

namespace MiyakoCarryService.Client.Mgrs
{
    public class ExfilDataMgr : GameWorldDataMgr
    {
        public ExfilDataMgr(IntPtr pointer) : base(pointer)
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        private readonly List<FinallExitZone> _finalExitZones = new();

        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            LoadData(LoadExfiltrationPoints);
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            InteropRegistry<FinallExitZone>.Clear();
        }

        private void LoadExfiltrationPoints()
        {
            Singleton<GameWorld>.Instance.AllPlayersEverExisted.ExecuteForEach(new Action<Player>((Player player) =>
            {
                if (!player.IsYourPlayer)
                {
                    return;
                }

                var exfiltrationController = Singleton<GameWorld>.Instance.ExfiltrationController;
                if (player.Profile.Side != EPlayerSide.Savage)
                {
                    var exfiltrationPoints = exfiltrationController.EligiblePoints(player.Profile);

                    if (exfiltrationPoints == null || exfiltrationPoints.Length == 0)
                    {
                        exfiltrationPoints = exfiltrationController.ExfiltrationPoints;
                    }

                    foreach (var exfiltrationPoint in exfiltrationPoints)
                    {
                        var data = exfiltrationPoint.GetData();
                        if (data != null)
                        {
                            _datas.Add(data);
                        }
                    }
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

                InteropRegistry<FinallExitZone>.CopyTo(_finalExitZones);
                foreach (var finalExitZone in _finalExitZones)
                {
                    if (finalExitZone == null)
                    {
                        continue;
                    }

                    var data = finalExitZone.GetData();
                    if (data != null)
                    {
                        _datas.Add(data);
                    }
                }
            }));
        }
    }
}