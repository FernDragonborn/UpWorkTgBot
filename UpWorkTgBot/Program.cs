using System.Xml;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace UpWork_bot;

internal class Post
{
    public Post()
    {
        Title = "N\\A";
        Description = "N\\A";
        PubDate = "N\\A";
    }

    internal string Title { get; set; }
    internal string Description { get; set; }
    internal string PubDate { get; set; }
}
//internal class Telegram
//{
//    TelegramBotClient botClient = new TelegramBotClient("5233647141:AAE8RtgZfUTh1Nxl8NF9e-uXFbwtpoikPvE");

//    public Func<ITelegramBotClient, Update, CancellationToken, Task> HandleUpdateAsync { get; private set; }
//    public Func<ITelegramBotClient, Exception, CancellationToken, Task> HandlePollingErrorAsync { get; private set; }
//    CancellationTokenSource cts = new CancellationTokenSource();

//    internal async Task start()
//    {
//        var receiverOptions = new ReceiverOptions
//        {
//            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
//        };
//        botClient.StartReceiving(
//            updateHandler: HandleUpdateAsync,
//            pollingErrorHandler: HandlePollingErrorAsync,
//            receiverOptions: receiverOptions,
//            cancellationToken: cts.Token
//        );
//        var me = await botClient.GetMeAsync();
//        Console.WriteLine($"Start listening for @{me.Username}");
//    }
//    internal async Task stop()
//    {
//        cts.Cancel();
//    }
//}
internal class Program
{
    static async Task Main()
    {
        //var tg = new Telegram();
        //tg.start();

        string url = "https://www.upwork.com/ab/feed/topics/rss?securityToken=a6ded1a910ace253e09c6ab3f7f5f9b0d2754bdd528a4453c2b2ac027a88ecbd8e3d448af8800faacbb544648922b3fd0fac7132b230f75d1be9c4f2e4c95a60&userUid=1583124974055403520&orgUid=1583124974055403521&topic=best-matches";
        var db = new DB(url);
        if (!(File.Exists("data.xml"))) db.downloadData();

        XmlDocument xDoc = new XmlDocument();
        xDoc.Load("data.xml");
        XmlElement? xChannel = xDoc.DocumentElement;
        XmlElement? xRoot = (XmlElement?)xChannel.FirstChild;
        var PostList = new List<Post>();
        if (xRoot is null) throw new NullReferenceException();
        foreach (XmlElement xnode in xRoot)
        {
            if (xnode.Name != "item") continue;
            Post post = new Post();
            foreach (XmlNode childnode in xnode.ChildNodes)
            {
                if (childnode.Name == "title")
                    post.Title = childnode.InnerText;
                if (childnode.Name == "description")
                    post.Description = childnode.InnerText;
                if (childnode.Name == "pubDate")
                    post.PubDate = childnode.InnerText;
            }
            if (post.Title is not null || post.Description is not null || post.PubDate is not null)
                PostList.Add(post);
        }
        //Console.WriteLine(data);
        Console.ReadKey();


        var botClient = new TelegramBotClient("{YOUR_ACCESS_TOKEN_HERE}");

        using var cts = new CancellationTokenSource();

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };
        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        // Send cancellation request to stop bot
        cts.Cancel();

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

            // Echo received message text
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "You said:\n" + messageText,
                cancellationToken: cancellationToken);
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
}

