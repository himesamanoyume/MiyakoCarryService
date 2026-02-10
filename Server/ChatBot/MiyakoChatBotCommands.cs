using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Interface;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace MiyakoCarryService.Server.ChatBot
{
    [Injectable]
    public class MiyakoChatBotCommands(IEnumerable<IMcsCommand> miyakoCommands) : IMcsChatCommand
    {
        protected readonly IDictionary<string, IMcsCommand> _miyakoCommands = miyakoCommands.ToDictionary(c => c.Command);

        public string[] GetCommandHelps(string command)
        {
            return _miyakoCommands.TryGetValue(command, out IMcsCommand value) ? value.CommandHelps : [];
        }

        public string CommandPrefix
        {
            get
            {
                return "mcs";
            }
        }

        public List<string> Commands
        {
            get
            {
                return [.. _miyakoCommands.Keys];
            }
        }

        public async ValueTask<string> Handle(string command, UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request)
        {
            return await _miyakoCommands[command].PerformAction(commandHandler, sessionId, request);
        }
    }
}