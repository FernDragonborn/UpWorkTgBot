using log4net;
using Newtonsoft.Json;
using System.Net;
using System.Xml;
using File = System.IO.File;

namespace UpWorkTgBot;

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
internal class DB
{
    static readonly ILog log = LogManager.GetLogger(typeof(Program));
    private static readonly string PART_DATA_PATH = DotNetEnv.Env.GetString("RSS_DOWNLOAD_PATH");
    private static readonly string DATA_PATH = $"{Directory.GetCurrentDirectory()}{PART_DATA_PATH}";
    private static readonly string PART_FREEL_PATH = DotNetEnv.Env.GetString("FREEL_PATH");
    private static readonly string FREEL_PATH = $"{Directory.GetCurrentDirectory()}{PART_FREEL_PATH}";

    public DB(string url)
    {
        this.Url = url;
    }
    internal string Url { get; set; }
    internal void downloadData()
    {
        if (!File.Exists(DATA_PATH)) { Directory.CreateDirectory(DATA_PATH.Replace("\\data.xml", "")); File.Create(DATA_PATH); }
        using var client = new WebClient();
        client.DownloadFile(Url, DATA_PATH);
    }

    static internal async Task SaveShownPosts(List<string> shownPosts, long chatId)
    {
        var tg = new Telegram();

        if (!(File.Exists(FREEL_PATH)))
        {
            await tg.SendMessageAsync(chatId, "Internal error 😥\nconnect @FernDragonborn");
            log.Fatal("[DB]: Failed to rewrite posts: json file not exists");
        }

        string content = File.ReadAllText(FREEL_PATH);
        List<Freelancer> freels = JsonConvert.DeserializeObject<List<Freelancer>>(content);
        if (freels.Count == 0)
        {
            await tg.SendMessageAsync(chatId, "DB doesn't exist 😥\nUse \"/start\" or if triend connect @FernDragonborn");
            log.Fatal($"[DB]: Failed to add RssUrl: freelansers count in list is 0");
            return;
        }
        foreach (Freelancer freel in freels)
        {
            if (freel.ChatId == chatId)
            {
                freel.ShownPosts = shownPosts;
                string json = JsonConvert.SerializeObject(freels);
                File.WriteAllText(FREEL_PATH, json);
                log.Info($"[DB]: Rewrited shown posts for {freel.Name}");
                return;
            }
        }
    }
    internal List<Post> GetPosts()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.Load(DATA_PATH);
        XmlElement? xChannel = xDoc.DocumentElement;
        XmlElement? xRoot = xChannel?.FirstChild as XmlElement;
        var PostList = new List<Post>();
        if (xRoot is null) throw new NullReferenceException();
        foreach (XmlElement xnode in xRoot)
        {
            if (xnode.Name != "item") continue;
            Post post = new();
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
        PostList.Reverse(); //for chronological order
        return PostList;
    }

    static internal List<Freelancer> GetFreelancers()
    {
        string content = File.ReadAllText(FREEL_PATH);
        return JsonConvert.DeserializeObject<List<Freelancer>>(content);
    }

    static internal async Task AddRssUrlAsync(string messageText, long chatId)
    {
        var tg = new Telegram();

        string[] parts = messageText.Split(' ');
        if (parts.Length != 3)
        {
            await tg.SendMessageAsync(chatId, "Invalid input, try ine more time. Syntax:\n/AddRssUrl <name> <url>\n\n*<> is for required argument");
            log.Debug($"[DB]: chatId: \"{chatId}\" tried to add RSS, but failed");
            return;
        }
        if (!(File.Exists(FREEL_PATH)))
        {
            await tg.SendMessageAsync(chatId, "DB doesn't exist 😥\nUse \"/start\" or if triend connect @FernDragonborn");
            log.Fatal("[DB]: Failed to add RssUrl: json file not exists");
            return;
        }

        string RssName = parts[1];
        string RssUrl = parts[2];

        string content = File.ReadAllText(FREEL_PATH);
        var freels = JsonConvert.DeserializeObject<List<Freelancer>>(content);
        if (freels.Count == 0 || freels is null)
        {
            await tg.SendMessageAsync(chatId, "DB doesn't exist 😥\nUse \"/start\" or if triend connect @FernDragonborn");
            log.Fatal("[DB]: Failed to add RssUrl: freelansers count in list is 0 or null");
            return;
        }
        foreach (Freelancer freel in freels)
        {
            if (freel.ChatId == chatId && DoesContainUrl(freel, parts))
            {
                await tg.SendMessageAsync(chatId, "you already have this rss url or rss with same name");
                return;
            }
            else if (freel.ChatId == chatId && freel.RssStrings is not null)
            {
                var Rss = new string[2] { RssName, RssUrl };
                freel.RssStrings.Add(Rss);
                string json = JsonConvert.SerializeObject(freels);
                File.WriteAllText(FREEL_PATH, json);
                log.Info($"[DB]: Added new RSS to {freel.Name}");
                await tg.SendMessageAsync(chatId, "Added RSS succesfully 😎");
                return;
            }
        }
    }
    static internal bool DoesContainUrl(Freelancer freel, string[] parts)
    {
        if (freel.RssStrings is null) return false;
        foreach (var rssString in freel.RssStrings)
        {
            if (rssString[0] == parts[1])
            {
                return true;
            }
            if (rssString[1] == parts[2])
            {
                return true;
            }
        }
        return false;
    }

    static internal async Task CreatNewFreelancerAsync(string name, long chatId)
    {
        var freelancer = new Freelancer(name, chatId);
        var tg = new Telegram();

        if (!(Directory.Exists("data"))) Directory.CreateDirectory("data");
        if (!(File.Exists(FREEL_PATH)))
        {
            File.Create(FREEL_PATH).Close();
            var freels2 = new List<Freelancer> { freelancer };
            string json2 = JsonConvert.SerializeObject(freels2);
            File.WriteAllText(FREEL_PATH, json2);
            return;
        }

        string content = File.ReadAllText(FREEL_PATH);
        List<Freelancer> freels = JsonConvert.DeserializeObject<List<Freelancer>>(content);
        if (freels.Count != 0 && chatId != 0)
        {
            foreach (Freelancer freel in freels)
            {
                if (freel.Name == name)
                {
                    await tg.SendMessageAsync(chatId, "Your profile already exists 😊");
                    log.Info($"{name} tried to add profile secondary");
                    return;
                }
            }
        }

        freels.Add(freelancer);
        string json = JsonConvert.SerializeObject(freels);
        File.WriteAllText(FREEL_PATH, json);
        await tg.SendMessageAsync(chatId, "Succesfully created your profile 😊");
        log.Info("[DB]: Created new freelancer");
    }

}

