

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFT;
using MiyakoCarryService.Client.Models;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace MiyakoCarryService.Client.Utils
{
    internal static class McsRequestHandler
    {
        private static T2 PostJson<T1, T2>(string path, T1 t1)
        {
            return Task.Run(() => PostJsonAsync<T1, T2>(path, t1)).GetAwaiter().GetResult();
        }

        private static async Task<T2> PostJsonAsync<T1, T2>(string path, T1 t1)
        {
            var serialized = JsonConvert.SerializeObject(t1);
            var response = await RequestHandler.PostJsonAsync(path, serialized);
            var data = JsonConvert.DeserializeObject<T2>(response);
            return data;
        }

        private static async Task PostJsonAsync<T1>(string path, T1 t1)
        {
            var serialized = JsonConvert.SerializeObject(t1);
            await RequestHandler.PostJsonAsync(path, serialized);
        }

        private static async Task PostJsonAsync(string path)
        {
            await RequestHandler.PostJsonAsync(path, "");
        }

        private static T GetJson<T>(string path)
        {
            return Task.Run(() => GetJsonAsync<T>(path)).GetAwaiter().GetResult();
        }

        private static async Task<T> GetJsonAsync<T>(string path)
        {
            var response = await RequestHandler.GetJsonAsync(path);
            var data = JsonConvert.DeserializeObject<T>(response);
            return data;
        }

        private static string PutJson<T>(string path, T t)
        {
            return Task.Run(() => PutJsonAsync(path, t)).GetAwaiter().GetResult();
        }

        private static async Task<string> PutJsonAsync<T>(string path, T t)
        {
            var serialized = JsonConvert.SerializeObject(t);
            var response = await RequestHandler.PutJsonAsync(path, serialized);
            return response;
        }

        public static async Task<Dictionary<MongoID, Profile[]>> GetMcsBotPlayers(SpawnMcsBotPlayerType spawnMcsBotPlayerType)
        {
            var response = await PostJsonAsync<SpawnMcsBotPlayerType, Dictionary<MongoID, CompleteProfileDescriptorClass[]>>("/mcs/client/game/bot/generate", spawnMcsBotPlayerType);

            if (response == null)
            {
                return new();
            }

            return response.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(desc => new Profile(desc)).ToArray()
            );
        }

        public static async Task<Dictionary<MongoID, McsBotPlayerConfig>> GetMcsBotPlayerConfigs()
        {
            var response = await GetJsonAsync<Dictionary<MongoID, McsBotPlayerConfig>>("/mcs/singleplayer/settings/bot/get");

            if (response == null)
            {
                return new();
            }
            
            return response;
        }

        public static async Task UploadMcsBotPlayerConfig(McsBotPlayerConfig mcsBotPlayerConfig)
        {
            await PostJsonAsync("/mcs/singleplayer/settings/bot/upload", mcsBotPlayerConfig);
        }

        public static async Task ClearGroupMember()
        {
            await PostJsonAsync("/mcs/client/match/raid/abort");
        }

        public static async Task SendPunishRequest(FriendlyFirePenalty friendlyFirePenalty)
        {
            await PostJsonAsync("/mcs/client/trading/api/friendlyFirePenalty", friendlyFirePenalty);
        }

        public static async Task<List<MongoID>> GetMcsBotPlayerIds()
        {
            var response = await GetJsonAsync<List<MongoID>>("/mcs/singleplayer/info/bot/get");
            
            if (response == null)
            {
                return new();
            }
            
            return response;
        } 
    }
}