using Comfort.Common;
using EFT;
using System;
using SPT.Reflection.Utils;
using MiyakoCarryService.Client.Utils;
using MiyakoCarryService.Client.Patches.GameWorldEvent;

namespace MiyakoCarryService.Client
{
    internal sealed class GameLoop : MiyakoCarryServiceSingleton<GameLoop>
    {
        public Player MyPlayer { get; private set; } = null;
        public PlayerOwner MyPlayerOwner { get; private set; } = null;

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
        }

        public void Init()
        {

        }
    }
}
