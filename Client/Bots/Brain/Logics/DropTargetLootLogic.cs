using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class DropTargetLootLogic : McsBotBaseLogic
    {
        public GoToPointBaseLogic _baseLogic;
        public float _nextCheckTime = Time.time;
        public DropTargetLootLogic(BotOwner botOwner) : base(botOwner)
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

            var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(mcsBotPlayerData);
            var sqrDistance = BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos);

            if (sqrDistance > 9f)
            {
                BotOwner.SetTargetMoveSpeed(1f);
                BotOwner.Sprint(true, false);
                BotOwner.SetPose(1f);
                BotOwner.Steering.LookToMovingDirection();
                return;
            }
            else
            {
                BotOwner.StopMove();
                BotOwner.Steering.LookToPoint(mcsBotPlayerData.LeadPlayer.MainParts[BodyPartType.head].Position);
                if (Time.time > _nextCheckTime)
                {
                    _nextCheckTime = Time.time + 1f;
                    TasksExtensions.HandleExceptions(DropTargetLoot(mcsBotPlayerData));
                }
            }
        }

        public async Task DropTargetLoot(McsBotPlayerData mcsBotPlayerData)
        {
            await Task.Delay(500);
            var wantDropItemId = BotOwner.ExternalItemsController.GetRandomItemToDrop();
            Item wantDropItem;
            try
            {
                wantDropItem = mcsBotPlayerData.Player.InventoryController.FindItem<Item>(wantDropItemId);
            }
            catch 
            {
                BotOwner.ExternalItemsController._pickUpedItems.Remove(wantDropItemId);
                return;
            }

            if (wantDropItem == null)
            {
                return;
            }

            var wantDropItemData = wantDropItem.GetData();
            if (wantDropItemData is not LootData wantDropLootData)
            {
                return;
            }
            
            BotOwner.ExternalItemsController._pickUpedItems.Remove(wantDropLootData.Item.Id);
            mcsBotPlayerData.Player.InventoryController.ThrowItem(wantDropLootData.Item);
            await Task.Delay(500);
        }
    }
}