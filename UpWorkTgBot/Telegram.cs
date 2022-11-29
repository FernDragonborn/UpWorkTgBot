using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace UpWorkTgBot;

internal class Telegram
{
    private static readonly string token = DotNetEnv.Env.GetString("TG_TOKEN");
    private static readonly string ADMIN_TOKEN = DotNetEnv.Env.GetString("ADMIN_TOKEN");

    private readonly TelegramBotClient botClient = new TelegramBotClient(token);
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

            //user commands
            if (messageText == "/start") { await DB.CreatNewFreelancerAsync(message.From.Username, chatId); await SendMessageAsync(chatId, "Hello, this bot can receive posts from UpWork directly to this chat in Telegram. Add your RSS link and get updates as soon as it possible!\n\nBot created by @FernDragonborn, connect him if help nedded and thanks for usage 🥰"); }
            else if (messageText == "/help") { await SendMessageAsync(chatId, "I can help you get new posts from UpWork immdeately!\n\nAvailable commnds:\n/start - start the bot and creates your profile\n/addRssUrl [url] - addes a new RSS location to receive posts. You can ahve as much as you want of them"); }
            else if (messageText.StartsWith("/addRssUrl")) { await DB.AddRssUrlAsync(messageText, chatId); }

            //admin commands
            if (chatId.ToString() == ADMIN_TOKEN)
            {
                if (messageText == "/sendDB")
                {
                    await using Stream stream = System.IO.File.OpenRead(@"freelancers.json");
                    Message sendFile = await botClient.SendDocumentAsync(
                        chatId: chatId,
                        document: new InputOnlineFile(content: stream, fileName: $"{DateTime.Now} freelancers backup.json"),
                        caption: $"#BackUp");
                }
                else if (messageText == "/sendEnv")
                {
                    await using Stream stream = System.IO.File.OpenRead(@".env");
                    Message sendFile = await botClient.SendDocumentAsync(
                        chatId: chatId,
                        document: new InputOnlineFile(content: stream, fileName: $".env")
                        );
                }
                else if (messageText == "/testAddRss")
                {
                    Console.WriteLine("try to test addRss");
                    await DB.AddRssUrlAsync("/addRssUrl https://www.upwork.com/ab/feed/jobs/rss?api_params=1&amp;budget=100-499%2C500-999%2C1000-4999%2C5000-&amp;job_type=hourly%2Cfixed&amp;ontology_skill_uid=1031626756493656064&amp;orgUid=1526554921826770945&amp;paging=0%3B10&amp;proposals=0-4%2C5-9&amp;q=&amp;securityToken=be3fbd85f54c1b3c626d21e8815a06b4fd21b61c4dc2f2664ce0109112f6c4a9f3e3504d635eba812bb1d38e21af9fe2661570c91ad8e396abdd056cfbc91559&amp;sort=recency&amp;userUid=1526554921826770944&amp;verified_payment_only=1&amp;workload=as_needed%2Cpart_time", 561838359);
                }

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
        sb.Append($"\n\n<b>Description: </b>\n{post.Description}\n");
        sb.Append($"\n<b>Publicated: </b>\n{post.PubDate}");
        sb.Replace("<br />", "\n");
        sb.Replace("\n\n", "\n");
        await SendMessageAsync(freel, sb.ToString());
        Console.WriteLine($"post {post.PubDate.Replace("+0000", "")} sended to {freel.Name}");
    }
}

