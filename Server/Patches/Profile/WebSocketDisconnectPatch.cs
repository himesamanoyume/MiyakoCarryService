using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using HarmonyLib;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Servers.Ws;

namespace MiyakoCarryService.Server.Patches.Group
{
    /// <summary>  
    /// 玩家断开连接时，若仍处于护航库存模式，则额外进行清理工作  
    /// </summary>  
    public sealed class WebSocketDisconnectPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SptWebSocketConnectionHandler), nameof(SptWebSocketConnectionHandler.OnClose));

        private static ProfileController ProfileController { get => field ??= ServiceLocator.ServiceProvider.GetService<ProfileController>(); }

        [PatchPrefix]
        public static void Prefix(WebSocket ws, HttpContext context, string sessionIdContext)
        {
            try
            {
                var sessionIdStr = context.Request.Path.Value.Split('/').Last();
                if (string.IsNullOrEmpty(sessionIdStr))
                {
                    return;
                }

                var sessionID = new MongoId(sessionIdStr);
                if (!ProfileController.IsMcsBotPlayerInventoryMode(sessionID))
                {
                    return;
                }

                ProfileController.SaveAllMcsBotPlayerProfile(sessionID).GetAwaiter().GetResult();
                ProfileController.RemoveMcsBotPlayerAid(sessionID);
            }
            catch
            {
                
            }
        }
    }
}