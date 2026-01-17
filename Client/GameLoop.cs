using Comfort.Common;
using EFT;
using System;
using SPT.Reflection.Utils;
using MiyakoCarryService.Client.Utils;
using MiyakoCarryService.Client.Patches.GameWorldEvent;
using System.Collections.Generic;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Mgrs;

namespace MiyakoCarryService.Client
{
    internal sealed class GameLoop : MiyakoCarryServiceSingleton<GameLoop>
    {
        public Player MyPlayer { get; private set; } = null;
        public Dictionary<Type, IMgr> Mgrs = new();

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
            IsVaildGameWorld = Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is not HideoutGameWorld;
            return IsVaildGameWorld;
        }

        public Action OnGameWorldStart
        {
            get
            {
                return GameWorldStartPatch.OnGameWorldStart;
            }

            set
            {
                GameWorldStartPatch.OnGameWorldStart = value;
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
            SquadMgr.Enable();
            BrainMgr.Enable();
        }

        private void Reset()
        {
            MyPlayer = null;
        }

        public T GetMgr<T>() where T : IMgr
        {
            return (T)Mgrs[typeof(T)];
        }
    }
}
