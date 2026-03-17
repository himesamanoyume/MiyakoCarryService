
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT;
using MiyakoCarryService.Client.Bots.BotBehaviors;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Mgrs;
using UnityEngine;

namespace MiyakoCarryService.Client.Extensions
{
    internal static class BotEnemyChooserExtensions
    {
        private static PlayerDataMgr PlayerDataMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<PlayerDataMgr>();
            }
        }

        private static McsMgr McsMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

        extension(BotEnemyChooser enemyChooser)
        {
            
        }
    }
}