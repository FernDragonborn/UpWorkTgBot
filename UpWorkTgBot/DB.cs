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
        this.url = url;
    }
    internal string url { get; set; }
    internal void downloadData()
    {
        //TODO repace / with sth, caise linux uses \
        if (!File.Exists(DATA_PATH)) { Directory.CreateDirectory(DATA_PATH.Replace("\\data.xml", "")); File.Create(DATA_PATH); }
        using var client = new WebClient();
        client.DownloadFile(url, DATA_PATH);
        //source = client.DownloadString(url);
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
        if (freels.Count != 0)
        {
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
        else
        {
            await tg.SendMessageAsync(chatId, "DB doesn't exist 😥\nUse \"/start\" or if triend connect @FernDragonborn");
            log.Fatal($"[DB]: Failed to add RssUrl: freelansers count in list is 0");
        }
    }

    static internal List<Freelancer> GetFreelancers()
    {
        string content = File.ReadAllText(FREEL_PATH);
        List<Freelancer> freels = JsonConvert.DeserializeObject<List<Freelancer>>(content);
        return freels;
    }

    //TODO add checks for input correctness
    static internal async Task AddRssUrlAsync(string messageText, long chatId)
    {
        var tg = new Telegram();

        if (!(File.Exists(FREEL_PATH)))
        {
            await tg.SendMessageAsync(chatId, "DB doesn't exist 😥\nUse \"/start\" or if triend connect @FernDragonborn");
            log.Fatal("[DB]: Failed to add RssUrl: json file not exists");
        }

        //TODO check if works normally (especially RssUrl)
        string RssName = messageText.TrimStart().Substring(11);
        int SpaceIndex = RssName.IndexOf(" ");
        RssName = RssName.Remove(SpaceIndex, RssName.Length - SpaceIndex);
        string RssUrl = messageText.TrimStart().Substring(11 + RssName.Length);

        string content = File.ReadAllText(FREEL_PATH);
        List<Freelancer> freels = JsonConvert.DeserializeObject<List<Freelancer>>(content);
        if (freels.Count != 0 && freels is not null)
        {
            foreach (Freelancer freel in freels)
            {
                if (freel.ChatId == chatId && freel.RssStrings is not null)
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
        else
        {
            await tg.SendMessageAsync(chatId, "DB doesn't exist 😥\nUse \"/start\" or if triend connect @FernDragonborn");
            log.Fatal("[DB]: Failed to add RssUrl: freelansers count in list is 0");
        }
    }

    static internal async Task CreatNewFreelancerAsync(string name, long chatId)
    {
        if (!(Directory.Exists("data"))) Directory.CreateDirectory("data");
        var freelancer = new Freelancer(name, chatId);
        var tg = new Telegram();

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

    internal List<Post> getPosts()
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
        PostList.Reverse(); //for chronological order
        return PostList;
    }
}

