
using System.Collections.Generic;
using System.Linq;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace MiyakoCarryService.Server.ChatBot
{
    [Injectable]
    public class MiyakoChatBot(ISptLogger<AbstractDialogChatBot> logger, MailSendService mailSendService,
    ServerLocalisationService localisationService, IEnumerable<MiyakoChatBotCommands> chatCommands)
    : AbstractDialogChatBot(logger, mailSendService, localisationService, chatCommands)
    {
        private readonly Dictionary<string, MiyakoChatBotCommands> _miyakoCommands = chatCommands.ToDictionary(c => c.CommandPrefix);
        private static readonly MongoId _miyakoId = new("686d2f9a3e1b4c8d2a5f0e7c");

        public override UserDialogInfo GetChatBot()
        {
            return new UserDialogInfo
            {
                Id = _miyakoId,
                Aid = 10107,
                Info = new()
                {
                    Level = 9999,
                    MemberCategory = MemberCategory.Sherpa,
                    SelectedMemberCategory = MemberCategory.Sherpa,
                    Nickname = "Tsukiyuki Miyako",
                    Side = "Usec",
                },
            };
        }

        protected override string GetUnrecognizedCommandMessage()
        {
            return "未知指令! 输入 \"help\" 来查看可用指令。";
        }
    }
}