using Comfort.Common;
using EFT;
using System;
using SPT.Reflection.Utils;
using MiyakoCarryService.Client.Utils;
using MiyakoCarryService.Client.Patches.GameWorldEvent;
using System.Collections.Generic;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Mgrs;

namespace MiyakoCarryService.Client
{
    internal sealed class GameLoop : MiyakoCarryServiceSingleton<GameLoop>
    {
        public Player MyPlayer { get; private set; } = null;
        public Dictionary<EMgrType, IMgr> Mgrs = new();

        public ISession Session
        {
            get
            {
                return field ??= ClientAppUtils.GetMainApp().GetClientBackEndSession();
            }
        }

        public bool IsVaildGameWorld = false;

        public bool CheckVaildGameWorld()
        {
            IsVaildGameWorld = Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is not HideoutGameWorld && IsGameStarted;
            return IsVaildGameWorld;
        }

        public bool IsGameStarted = false;


        public Action OnGameWorldStarted
        {
            get
            {
                return GameWorldOnGameStartedPatch.OnGameWorldStarted;
            }

            set
            {
                GameWorldOnGameStartedPatch.OnGameWorldStarted = value;
            }
        }

        public Action OnGameWorldDestory
        {
            get
            {
                return RaidEndedPatch.OnGameWorldDestory;
            }

            set
            {
                RaidEndedPatch.OnGameWorldDestory = value;
            }
        }

        void Update()
        {
            CheckVaildGameWorld();
            // if (Tools.CheckGameWorld())
            // {
            //     if (MyPlayer == null)
            //     {
            //         var mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            //         if (mainPlayer == null)
            //         {
            //             return;
            //         }

            //         MyPlayer = mainPlayer;
            //     }
            // }
        }

        public void Init()
        {
            BotMgr.Enable();
        }

        private void Reset()
        {
            MyPlayer = null;
        }

        public T GetMgr<T>(EMgrType type) where T : IMgr
        {
            return (T)Mgrs[type];
        }
    }
}
