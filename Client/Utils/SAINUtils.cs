using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using EFT;

namespace MiyakoCarryService.Client.Utils
{
    internal static class SAINUtils
    {
        public static readonly Type PlayerComponentType = Type.GetType("SAIN.Components.PlayerComponentSpace.PlayerComponent, SAIN") ?? Type.GetType("SAIN.Components.PlayerComponent, SAIN");

        private static readonly Func<object, BotOwner> _playerComponentBotOwnerGetter = BuildBotOwnerGetter(PlayerComponentType);
        private static readonly ConditionalWeakTable<object, BotOwner> _playerComponentBotOwners = new();

        public static BotOwner GetPlayerComponentBotOwner(object playerComponent)
        {
            if (playerComponent == null)
            {
                return null;
            }

            if (_playerComponentBotOwners.TryGetValue(playerComponent, out var cached))
            {
                return cached;
            }

            var botOwner = _playerComponentBotOwnerGetter?.Invoke(playerComponent);
            if (botOwner == null)
            {
                return null;
            }

            _playerComponentBotOwners.Add(playerComponent, botOwner);
            return botOwner;
        }

        private static Func<object, BotOwner> BuildBotOwnerGetter(Type type)
        {
            if (type == null)
            {
                return null;
            }

            try
            {
                var param = Expression.Parameter(typeof(object), "o");
                var cast = Expression.Convert(param, type);
                var access = (Expression)(type.GetProperty("BotOwner") != null ? Expression.Property(cast, "BotOwner") : Expression.Field(cast, "BotOwner"));
                return Expression.Lambda<Func<object, BotOwner>>(Expression.Convert(access, typeof(BotOwner)), param).Compile();
            }
            catch (Exception e)
            {
                McsLogger.LogError(e);
                return null;
            }
        }
    }
}