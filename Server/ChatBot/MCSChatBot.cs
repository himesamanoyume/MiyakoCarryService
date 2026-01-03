
using System.Collections.Generic;
using System.Linq;
using MiyakoCarryService.Server.Services;
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
    public class MCSChatBot(
        ISptLogger<AbstractDialogChatBot> logger, 
        MailSendService mailSendService, 
        ServerLocalisationService localisationService, 
        IEnumerable<MCSChatBotCommands> chatCommands
    ) : AbstractDialogChatBot(logger, mailSendService, localisationService, chatCommands)
    {
        private static readonly MongoId _miyakoId = new(MCSTraderService.MiyakoTraderId);

        public override UserDialogInfo GetChatBot()
        {
            return new UserDialogInfo
            {
                Id = _miyakoId,
                Aid = 10107,
                Info = new()
                {
                    Level = 999,
                    MemberCategory = MemberCategory.Trader,
                    SelectedMemberCategory = MemberCategory.Trader,
                    Nickname = "Miyako",
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