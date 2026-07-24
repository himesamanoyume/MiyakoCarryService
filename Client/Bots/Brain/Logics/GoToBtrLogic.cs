using System;
using System.Threading.Tasks;
using Comfort.Common;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.Vehicle;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class GoToBtrLogic : McsBotBaseLogic
    {
        private GoToPointBaseLogic _baseLogic;
        private int _currentRetries = 0;
        private float _lastTimeCheckDistance = 0f;

        public GoToBtrLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);

            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return;
            }

            if (mcsBotPlayerData.IsTaskRunning)
            {
                return;
            }

            var btrController = Singleton<GameWorld>.Instance.BtrController;
            if (btrController == null || btrController.BtrVehicle == null || btrController.BtrView == null)
            {
                return;
            }

            if (mcsBotPlayerData.IsBtrLeaving)
            {
                TasksExtensions.HandleExceptions(LeaveBtr());
                return;
            }

            BotOwner.SetTargetMoveSpeed(1f);
            BotOwner.Sprint(true, false);
            BotOwner.SetPose(1f);
            BotOwner.Steering.LookToMovingDirection();

            if (_lastTimeCheckDistance < Time.time)
            {
                _currentRetries++;
                if (_currentRetries > 20)
                {
                    _currentRetries = 0;
                    return;
                }
                _lastTimeCheckDistance = Time.time + 1f;

                var side = btrController.BtrView.GetBtrSide(mcsBotPlayerData.BtrTargetSide);
                if (side == null)
                {
                    return;
                }

                var doorPos = side.GoInPoints().Item1;
                var offset = BotOwner.Position - doorPos;
                var sqrDistance = BotOwner.Position.McsSqrDistance(doorPos);

                if (sqrDistance <= 9f && Math.Abs(offset.y) < 3f)
                {
                    BotOwner.SetTargetMoveSpeed(0f);
                    BotOwner.Steering.LookToPoint(doorPos);
                    TasksExtensions.HandleExceptions(BoardBtr());
                    return;
                }
            }
        }

        private async Task BoardBtr()
        {
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            try
            {
                if (mcsBotPlayerData == null)
                {
                    return;
                }

                var btrController = Singleton<GameWorld>.Instance.BtrController;
                if (btrController == null || btrController.BtrVehicle == null)
                {
                    return;
                }

                var btrVehicle = btrController.BtrVehicle;
                var player = BotOwner.GetPlayer;

                if (btrVehicle.IsPassenger(player, out _))
                {
                    return;
                }

                mcsBotPlayerData.IsTaskRunning = true;

                var packet = new InteractWithBtrPacket
                {
                    HasInteraction = true,
                    InteractionType = EInteractionType.GoIn,
                    SideId = mcsBotPlayerData.BtrTargetSide,
                    SlotId = mcsBotPlayerData.BtrTargetSlot,
                    Fast = false
                };


                var status = btrVehicle.InteractInternal(player, packet);
                if (status != EBtrInteractionStatus.Confirmed)
                {

                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
            }
            finally
            {
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.IsTaskRunning = false;
                }
            }
        }

        private async Task LeaveBtr()
        {
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            try
            {
                if (mcsBotPlayerData == null)
                {
                    return;
                }

                var btrController = Singleton<GameWorld>.Instance.BtrController;
                if (btrController == null || btrController.BtrVehicle == null)
                {
                    return;
                }

                var btrVehicle = btrController.BtrVehicle;
                var player = BotOwner.GetPlayer;

                if (!btrVehicle.IsPassenger(player, out var passenger))
                {
                    mcsBotPlayerData.IsBtrLeaving = false;
                    return;
                }

                mcsBotPlayerData.IsTaskRunning = true;

                var packet = new InteractWithBtrPacket
                {
                    HasInteraction = true,
                    InteractionType = EInteractionType.GoOut,
                    SideId = passenger.SideId,
                    SlotId = passenger.SlotId,
                    Fast = false
                };

                var status = btrVehicle.InteractInternal(player, packet);
                if (status == EBtrInteractionStatus.Confirmed || status == EBtrInteractionStatus.EmptySlot)
                {
                    mcsBotPlayerData.IsBtrLeaving = false;
                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
            }
            finally
            {
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.IsTaskRunning = false;
                }
            }
        }
    }
}