using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace UpWorkTgBot;

internal class Telegram
{
    TelegramBotClient botClient = new TelegramBotClient("5233647141:AAE8RtgZfUTh1Nxl8NF9e-uXFbwtpoikPvE");
    public Func<ITelegramBotClient, Update, CancellationToken, Task> HandleUpdateAsync { get; private set; }
    public Func<ITelegramBotClient, Exception, CancellationToken, Task> HandlePollingErrorAsync { get; private set; }
    CancellationTokenSource cts = new CancellationTokenSource();
    ReceiverOptions receiverOptions = new ReceiverOptions
    {
        AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
    };
    private readonly CancellationToken cancellationToken;

    internal async Task init()
    {
        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

            if (messageText == "/start") { DB.CreatNewFreelancer(message.From.Username, chatId); }

            // Echo received message text
            //Message sentMessage = await botClient.SendTextMessageAsync(
            //    chatId: chatId,
            //    text: "You said:\n" + messageText,
            //    cancellationToken: cancellationToken);
        }

        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        Task stop()
        {
            cts.Cancel();
            return Task.CompletedTask;
        }
    }
    public async Task SendMessageAsync(Freelancer freelancer, string messageText)
    {
        Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: freelancer.ChatId,
            text: messageText,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}

