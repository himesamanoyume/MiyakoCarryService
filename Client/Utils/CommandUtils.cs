
using System.Collections.Generic;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Utils
{
    internal static class CommandUtils
    {
        private static List<ICommand> _commandsMap;

        public static void RegisterCommand(ICommand command)
        {
            if (_commandsMap == null)
            {
                _commandsMap = new();
            }

            if (command == null)
            {
                return;
            }
            _commandsMap.Add(command);
        }

        public static List<ICommand> GetCommands()
        {
            if (_commandsMap == null)
            {
                _commandsMap = new();
            }

            return _commandsMap;
        }
    }
}