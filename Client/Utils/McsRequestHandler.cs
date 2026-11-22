

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EFT;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using SPTushonka.Common.Http;
using SPTushonka.Reflection.Il2Cpp;

namespace MiyakoCarryService.Client.Utils
{
    public static class McsRequestHandler
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { IncludeFields = true };

        public static T2 PostJson<T1, T2>(string path, T1 t1)
        {
            return Task.Run(() => PostJsonAsync<T1, T2>(path, t1)).GetAwaiter().GetResult();
        }

        public static async Task<T2> PostJsonAsync<T1, T2>(string path, T1 t1)
        {
            var serialized = JsonSerializer.Serialize(t1, _jsonOptions);
            var response = await RequestHandler.PostJsonAsync(path, serialized);
            var data = JsonSerializer.Deserialize<T2>(response, _jsonOptions);
            return data;
        }

        public static async Task PostJsonAsync<T1>(string path, T1 t1)
        {
            var serialized = JsonSerializer.Serialize(t1, _jsonOptions);
            await RequestHandler.PostJsonAsync(path, serialized);
        }

        public static async Task PostJsonAsync(string path)
        {
            await RequestHandler.PostJsonAsync(path, "");
        }

        public static T GetJson<T>(string path)
        {
            return Task.Run(() => GetJsonAsync<T>(path)).GetAwaiter().GetResult();
        }

        public static async Task<T> GetJsonAsync<T>(string path)
        {
            var response = await RequestHandler.GetJsonAsync(path);
            var data = JsonSerializer.Deserialize<T>(response, _jsonOptions);
            return data;
        }

        public static string PutJson<T>(string path, T t)
        {
            return Task.Run(() => PutJsonAsync(path, t)).GetAwaiter().GetResult();
        }

        public static async Task<string> PutJsonAsync<T>(string path, T t)
        {
            var serialized = JsonSerializer.Serialize(t, _jsonOptions);
            var response = await RequestHandler.PutJsonAsync(path, serialized);
            return response;
        }

        public static async Task<Dictionary<string, Profile[]>> GetMcsBotPlayers(McsBotPlayerType mcsBotPlayerType)
        {
            MainThread.VerifyAccess();
            var serialized = JsonSerializer.Serialize(mcsBotPlayerType, _jsonOptions);
            var json = await RequestHandler.PostJsonAsync("/mcs/client/game/bot/generate", serialized);
            return BuildMcsBotPlayers();

            Dictionary<string, Profile[]> BuildMcsBotPlayers()
            {
                var result = new Dictionary<string, Profile[]>();
                var response = DeserializeEftJson<Il2CppSystem.Collections.Generic.Dictionary<MongoID, Il2CppReferenceArray<ProfileDescriptor>>>(json);
                if (response == null)
                {
                    return result;
                }

                foreach (var kvp in response)
                {
                    result[kvp.Key.MongoIdText()] = kvp.Value.Select(desc => new Profile(desc)).ToArray();
                }

                return result;
            }
        }

        public static async Task<ProfileDescriptor[]> GetMcsBotPlayerProfiles()
        {
            MainThread.VerifyAccess();
            var json = await RequestHandler.GetJsonAsync("/mcs/client/game/profile/list");
            return DeserializeEftJson<Il2CppReferenceArray<ProfileDescriptor>>(json)?.ToArray() ?? [];
        }

        private static T DeserializeEftJson<T>(string json) where T : Il2CppObjectBase
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var serializer = HTTPTransportManager.GetResponseJsonSerializer();
            var jsonReader = new Newtonsoft.Json.JsonTextReader(new Il2CppSystem.IO.StringReader(json));
            return serializer.Deserialize(jsonReader, ManagedArguments.ToIl2CppType(typeof(T))).TryCast<T>();
        }

        public static bool VerifyMcsBotPlayerAid(McsBotPlayerAid mcsBotPlayerAid)
        {
            var response = PostJson<McsBotPlayerAid, bool>("/mcs/client/game/aid/verify", mcsBotPlayerAid);

            return response;
        }

        public static bool RemoveMcsBotPlayerAid(McsBotPlayerAid mcsBotPlayerAid)
        {
            var response = PostJson<McsBotPlayerAid, bool>("/mcs/client/game/aid/remove", mcsBotPlayerAid);

            return response;
        }

        public static async Task<ConcurrentDictionary<string, McsBotPlayerConfig>> GetMcsBotPlayerConfigs()
        {
            var response = await GetJsonAsync<ConcurrentDictionary<string, McsBotPlayerConfig>>("/mcs/singleplayer/settings/bot/get");

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

        public static async Task SendCompensationRequest(Compensation compensation)
        {
            await PostJsonAsync("/mcs/client/trading/api/compensation", compensation);
        }

        public static async Task<McsProfileChangesPayload> UpdateProfile()
        {
            var response = await GetJsonAsync<McsProfileChangesPayload>("/mcs/client/trading/api/updateProfile");

            if (response == null)
            {
                return new();
            }

            return response;
        }

        public static async Task<List<string>> RequestMySquadMcsBotPlayerIds(McsBotPlayerType mcsBotPlayerType)
        {
            var response = await PostJsonAsync<McsBotPlayerType, List<string>>("/mcs/singleplayer/info/bot/get", mcsBotPlayerType);

            if (response == null)
            {
                return new();
            }

            return response;
        }

        public static async Task SendLog(string text)
        {
            await PostJsonAsync("/mcs/client/log", new DebugInfo
            {
                Info = text
            });
        }

        public static async Task<HashSet<string>> GetAllMcsBotPlayerIdInRaid(McsBotPlayerType mcsBotPlayerType)
        {
            var response = await PostJsonAsync<McsBotPlayerType, HashSet<string>>("/mcs/singleplayer/info/botids/get", mcsBotPlayerType);

            if (response == null)
            {
                return new();
            }

            return response;
        }

        public static Dictionary<string, SPTServerModInfo> GetLoadedServerMods()
        {
            try
            {
                var response = GetJson<LauncherV2ModsResponse>("/launcher/v2/mods");
                return response?.Response ?? new Dictionary<string, SPTServerModInfo>();
            }
            catch
            {
                return new Dictionary<string, SPTServerModInfo>();
            }
        }

        public static McsPluginClientConfig GetMcsPluginClientConfig()
        {
            try
            {
                var response = GetJson<McsPluginClientConfig>("/mcs/client/config");
                return response;
            }
            catch
            {
                return new();
            }
        }

        public static bool SettleMcsOrder(McsBotPlayerAid mcsBotPlayerAid)
        {
            var response = PostJson<McsBotPlayerAid, bool>("/mcs/client/order/settle", mcsBotPlayerAid);

            return response;
        }

        public static bool RenewMcsOrder(McsBotPlayerAid mcsBotPlayerAid)
        {
            var response = PostJson<McsBotPlayerAid, bool>("/mcs/client/order/renew", mcsBotPlayerAid);

            return response;
        }


    }
}