using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace atcmdHost
{
    public class UpdateHandler : IUpdateHandler
    {
        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception ex, HandleErrorSource handleErrorSource, CancellationToken cancellationToken)
        {
            MainSystem.throwErr(ex.Message, "BOT_MESSAGE_RECEIVING_FAILURE!", true);
            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message?.Text == null) return;
            string message = update.Message?.Text;

            MainSystem.messageRecivied(message, update.Message.Chat.Id, update.Message.Chat.Username);

            //MessageBox.Show(message);
            //await botClient.SendMessage(update.Message.Chat.Id, "Вы отправили: " + message, ParseMode.None);
        }
    }
}