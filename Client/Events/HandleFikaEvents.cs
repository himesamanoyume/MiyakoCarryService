using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Models;
using UnityEngine;

namespace MiyakoCarryService.Client.Events
{
    public class SubtitlesMgrHandleFikaEvent : IMcsEvent
    {
        public MongoID McsLeadPlayerId { get; set; }
        public MongoID McsBotPlayerId { get; set; }
        public McsMsg Msg { get; set; }
    }

    public class QuestProxyCommandCallbackHandleFikaEvent : IMcsEvent
    {
        public MongoID McsLeadPlayerId { get; set; }
        public MongoID McsBotPlayerId { get; set; }
        public string TargetId { get; set; }
    }

    public class CommandMgrHandleFikaEvent : IMcsEvent
    {
        public Player McsBotPlayer { get; set; }
        public string CommandPacketType { get; set; }
        public Vector3? Position { get; set; }
        public BodyPartType AimingBodyPartType { get; set; }
        public string TargetId { get; set; }
        public Dictionary<string, McsValue> Extensions { get; set; }
    }

    public class ConfigEntrySettingChangedEvent : IMcsEvent
    {
        public McsBotPlayerConfig McsBotPlayerConfig { get; set; }
    }

    public class McsLeadPlayerExtractedEvent : IMcsEvent
    {
        public MongoID McsLeadPlayerId { get; set; }
    }
}