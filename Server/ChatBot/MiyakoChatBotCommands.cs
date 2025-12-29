using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Dialog.Commando;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace MiyakoCarryService.Server.ChatBot
{
    [Injectable]
    public class MiyakoChatBotCommands(IEnumerable<IMiyakoCommand> miyakoCommands) : IChatCommand
    {
        protected readonly IDictionary<string, IMiyakoCommand> _miyakoCommands = miyakoCommands.ToDictionary(c => c.Command);

        public string GetCommandHelp(string command)
        {
            return _miyakoCommands.TryGetValue(command, out IMiyakoCommand value) ? value.CommandHelp : string.Empty;
        }

        public string CommandPrefix
        {
            get
            {
                return "miyakocs";
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

    public interface IMiyakoCommand
    {
        public string Command { get; }
        public string CommandHelp { get; }
        public ValueTask<string> PerformAction(UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request);
    }
}