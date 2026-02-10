using System.Collections.Generic;
using System.Threading.Tasks;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace MiyakoCarryService.Server.Interface
{
    public interface IMcsCommand
    {
        public string Command { get; }
        public string[] CommandHelps { get; }
        public ValueTask<string> PerformAction(UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request);
    }

    public interface IMcsChatCommand
    {
        string CommandPrefix { get; }

        List<string> Commands { get; }

        string[] GetCommandHelps(string command);

        ValueTask<string> Handle(string command, UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request);
    }
}