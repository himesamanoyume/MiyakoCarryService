using EFT;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Models;
using UnityEngine;

namespace MiyakoCarryService.Client.Events
{
    public sealed class SubtitlesMgrHandleFikaEvent : IMcsEvent
    {
        public MongoID McsLeadPlayerId { get; set; }
        public MongoID McsBotPlayerId { get; set; }
        public McsMsg Msg { get; set; }
    }

    public sealed class CommandMgrHandleFikaEvent : IMcsEvent
    {
        public Player McsBotPlayer { get; set; }
        public ECommandPacketType CommandPacketType { get; set; }
        public Vector3? Position { get; set; }
        public BodyPartType AimingBodyPartType { get; set; }
        public string TargetId { get; set; }
    }

    public sealed class ConfigEntrySettingChangedEvent : IMcsEvent
    {
        public McsBotPlayerConfig McsBotPlayerConfig { get; set; }
    }

    public sealed class McsLeadPlayerExtractedEvent : IMcsEvent
    {
        public MongoID McsLeadPlayerId { get; set; }
    }

    public sealed class QuestProxyActionReadyToStartEvent : IMcsEvent
    {
        public MongoID McsLeadPlayerId { get; set; }
        public MongoID McsBotPlayerId { get; set; }
        public string TargetId { get; set; }
    }
}