using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.Communications;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions.ToIl2Cpp;
using MiyakoCarryService.Client.Patches.Group;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;
using UnityEngine;
using SPTushonka.Reflection.Il2Cpp;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 当进入护航库存模式时，拦截并改为获取护航的存档
    /// </summary>
    public sealed class GetProfilesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EftClientBackendSession), nameof(EftClientBackendSession.GetProfiles));

        [PatchPrefix]
        public static bool Prefix(EftClientBackendSession __instance, ref Il2CppSystem.Threading.Tasks.Task __result)
        {
            if (!GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                return true;
            }

            __result = GetMcsBotPlayerProfile(__instance);
            return false;
        }

        [PatchPostfix]
        public static void Postfix(EftClientBackendSession __instance, Il2CppSystem.Threading.Tasks.Task __result)
        {
            // 原来是 async void：续体会落到线程池（在那里改 AllProfiles 会 AV），且异常不可观测。
            // 现在改为普通方法 + 显式任务 + 主线程写回。
            RewriteProfileStatusesAsync(__instance, __result).McsHandleExceptions();
        }

        private static async Task RewriteProfileStatusesAsync(EftClientBackendSession instance, Il2CppSystem.Threading.Tasks.Task original)
        {
            await original.AsManaged();

            // 写游戏对象：original 已用 AsManaged() 桥接，续体回到主线程，可直接执行
            ApplyProfileStatuses();

            void ApplyProfileStatuses()
            {
                var profileStatuses = new List<ProfileStatus>();
                foreach (var profile in instance.AllProfiles)
                {
                    profileStatuses.Add(new()
                    {
                        profileid = profile.Id,
                        status = EProfileStatus.Free,
                    });
                }
                instance.AllProfileStatus = profileStatuses.ToArray().ToIl2Cpp();
            }
        }

        private static Il2CppSystem.Threading.Tasks.Task GetMcsBotPlayerProfile(EftClientBackendSession session)
        {
            var completion = new Il2CppSystem.Threading.Tasks.TaskCompletionSource<Il2CppSystem.Object>();

            async Task Load()
            {
                try
                {
                    var response = await McsRequestHandler.GetMcsBotPlayerProfiles();

                    // 同上：GetMcsBotPlayerProfiles() 是托管 Task，续体已在主线程
                    ApplyLoadedProfiles();

                    void ApplyLoadedProfiles()
                    {
                        if (response == null || response.Length == 0)
                        {
                            GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode = false;
                            NotificationManager.DisplayMessageNotification("未获取到护航存档，无法加载", textColor: new Il2CppSystem.Nullable<Color>());
                            return;
                        }

                        session.ProfilesUpdateTime = Time.time;
                        session.AllProfiles = response.Select(descriptor => new Profile(descriptor)).ToArray().ToIl2Cpp();
                        session.Profile = session.AllProfiles[0];
                    }
                }
                catch (System.Exception ex)
                {
                    McsLogger.LogError(ex);
                }
                finally
                {
                    // 前面的 await 续体在线程池：il2cpp 的 TrySetResult 必须包一层回主线程，否则闪退
                    MainThread.Post(() => completion.TrySetResult(null));
                }
            }

            Load().McsHandleExceptions();
            return completion.Task;
        }
    }
}
