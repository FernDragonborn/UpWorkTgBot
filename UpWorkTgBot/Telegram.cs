using System.Text;
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

            Console.WriteLine($"{DateTime.Now}  [TG]: Received a '{messageText}' message in chat {chatId}.");

            if (messageText == "/start") { await DB.CreatNewFreelancerAsync(message.From.Username, chatId); SendMessageAsync(chatId, "Hello, приветсвенного сообщения пока нет"); }
            if (messageText.StartsWith("/addRssUrl")) { DB.AddRssUrlAsync(messageText, chatId); }
            if (messageText.StartsWith("/test"))
            {
                Console.WriteLine("try to test addRss");
                await DB.AddRssUrlAsync("/addRssUrl https://www.upwork.com/ab/feed/jobs/rss?api_params=1&amp;budget=100-499%2C500-999%2C1000-4999%2C5000-&amp;job_type=hourly%2Cfixed&amp;ontology_skill_uid=1031626756493656064&amp;orgUid=1526554921826770945&amp;paging=0%3B10&amp;proposals=0-4%2C5-9&amp;q=&amp;securityToken=be3fbd85f54c1b3c626d21e8815a06b4fd21b61c4dc2f2664ce0109112f6c4a9f3e3504d635eba812bb1d38e21af9fe2661570c91ad8e396abdd056cfbc91559&amp;sort=recency&amp;userUid=1526554921826770944&amp;verified_payment_only=1&amp;workload=as_needed%2Cpart_time", 561838359);
            }

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
    public async Task SendMessageAsync(long chatId, string messageText)
    {
        Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: messageText,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
    public async Task SendPostAsync(Freelancer freel, Post post)
    {
        var sb = new StringBuilder();
        sb.Append($"<b>Title: </b>\n{post.Title}\n");
        sb.Append($"<b>Description: </b>\n{post.Description}\n");
        sb.Append($"<b>Publicated: </b>\n{post.PubDate}");
        sb.Replace("<br />", "\n");
        await SendMessageAsync(freel, sb.ToString());
    }
}

