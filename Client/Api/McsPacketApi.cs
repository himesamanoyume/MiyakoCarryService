

using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using Newtonsoft.Json;

namespace MiyakoCarryService.Client.Api
{
    public static class McsPacketApi
    {
        private static readonly JsonSerializerSettings _settings = new()
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string SerializeBotPlayerConfig(McsBotPlayerConfig config)
        {
            return JsonConvert.SerializeObject(config, _settings);
        }

        public static McsBotPlayerConfig DeserializeBotPlayerConfig(string json)
        {
            return JsonConvert.DeserializeObject<McsBotPlayerConfig>(json, _settings) ?? new McsBotPlayerConfig();
        }

        public static void ApplyBotPlayerConfig(MongoID mcsLeadPlayerId, string json)
        {
            var config = DeserializeBotPlayerConfig(json);
            config.McsLeadPlayerId = mcsLeadPlayerId;
            McsMgrApi.GetMgr<McsMgr>().UpdateMcsBotPlayerConfig(mcsLeadPlayerId, config);
        }

        public static string SerializeCommand(CommandMgrHandleFikaEvent @event)
        {
            var request = new McsCommandRequest
            {
                CommandType = @event.CommandPacketType,
                TargetId = @event.TargetId,
                Position = @event.Position,
                AimingBodyPartType = @event.AimingBodyPartType,
                ShouldCheckExclude = @event.ShouldCheckExclude,
                Extensions = @event.Extensions ?? new Dictionary<string, McsValue>()
            };
            return JsonConvert.SerializeObject(request, _settings);
        }

        public static void ExecuteCommand(string json, Player mcsLeadPlayer, Player mcsBotPlayer)
        {
            var request = JsonConvert.DeserializeObject<McsCommandRequest>(json, _settings);
            if (request == null)
            {
                return;
            }

            McsCommandApi.Execute(new McsCommandContext
            {
                McsLeadPlayer = mcsLeadPlayer,
                McsBotPlayer = mcsBotPlayer,
                CommandType = request.CommandType,
                TargetId = request.TargetId,
                Position = request.Position,
                AimingBodyPartType = request.AimingBodyPartType,
                ShouldCheckExclude = request.ShouldCheckExclude,
                Extensions = request.Extensions ?? new Dictionary<string, McsValue>()
            }, true);
        }

        public static string SerializeMsg(McsMsg msg)
        {
            return JsonConvert.SerializeObject(msg, _settings);
        }

        public static McsMsg DeserializeMsg(string json)
        {
            return JsonConvert.DeserializeObject<McsMsg>(json, _settings) ?? new McsMsg();
        }

        public static string SerializeQuestProxy(string targetId)
        {
            return JsonConvert.SerializeObject(new McsQuestProxyRequest
            {
                TargetId = targetId
            }, _settings);
        }

        public static string DeserializeQuestProxy(string json)
        {
            var request = JsonConvert.DeserializeObject<McsQuestProxyRequest>(json, _settings);
            return request?.TargetId;
        }
    }
}
