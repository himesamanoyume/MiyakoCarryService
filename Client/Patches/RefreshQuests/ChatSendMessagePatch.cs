
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.RefreshQuests
{
    /// <summary>
    /// 当可能发送下单相关的消息时进行主动请求刷新任务
    /// </summary>
    internal sealed class ChatSendMessagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ProfileEndpointFactoryAbstractClass), nameof(ProfileEndpointFactoryAbstractClass.ChatSendMessage));

        private static MainMenuControllerClass mainMenuControllerClass;

        [PatchPostfix]
        public static void Postfix(string id, int type, string text, string replyTo, Callback<string> callback)
        {
            if (text.Contains("mcs order"))
            {
                _ = UpdateDailyQuests();
            }
        }

        private static async Task UpdateDailyQuests()
        {
            if (mainMenuControllerClass == null)
            {
                TarkovApplication.Exist(out var tarkovApplication);
                var tarkovApplicationTraverse = Traverse.Create(tarkovApplication);
                mainMenuControllerClass = tarkovApplicationTraverse.Field<MainMenuControllerClass>("mainMenuControllerClass").Value;
            }

            await Task.Delay(2000); 

            // _ = mainMenuControllerClass?.LocalQuestControllerClass?.QuestBookClass?.Gclass4059_0?.Run();
            var array = await GameLoop.Instance.Session.GetDailyQuests();
            if (!array.IsNullOrEmpty())
            {
                mainMenuControllerClass.LocalQuestControllerClass.QuestBookClass.UpdateDailyQuests(array);
            }
        }
    }
}