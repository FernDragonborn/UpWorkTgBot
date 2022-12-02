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
    private static readonly log4net.ILog log = LogHelper.GetLogger();

    readonly string dataPath = "data.xml";
    public DB(string url)
    {
        this.url = url;
    }
    internal string url { get; set; }
    internal void downloadData()
    {
        using var client = new WebClient();
        client.DownloadFile(url, dataPath);
        //source = client.DownloadString(url);
    }

    static internal async Task SaveShownPosts(List<string> shownPosts, long chatId)
    {
        string path = "freelancers.json";
        var tg = new Telegram();

        if (!(File.Exists(path)))
        {
            await tg.SendMessageAsync(chatId, "Internal error 😥\nconnect @FernDragonborn");
            Console.WriteLine($"{DateTime.Now}\t[DB]: Failed to rewrite posts: json file not exists");
        }

        string content = File.ReadAllText(path);
        List<Freelancer> freels = JsonConvert.DeserializeObject<List<Freelancer>>(content);
        if (freels.Count != 0)
        {
            foreach (Freelancer freel in freels)
            {
                if (freel.ChatId == chatId)
                {
                    freel.ShownPosts = shownPosts;
                    string json = JsonConvert.SerializeObject(freels);
                    File.WriteAllText(path, json);
                    Console.WriteLine($"{DateTime.Now}\t[DB]: Rewrited shown posts for {freel.Name}");
                    return;
                }
            }
        }
        else
        {
            await tg.SendMessageAsync(chatId, "DB doesn't exist 😥\nUse \"/start\" or if triend connect @FernDragonborn");
            Console.WriteLine($"{DateTime.Now}\t[DB]: Failed to add RssUrl: freelansers count in list is 0");
        }
    }

    static internal List<Freelancer> GetFreelancers()
    {
        string path = "freelancers.json";

        string content = File.ReadAllText(path);
        List<Freelancer> freels = JsonConvert.DeserializeObject<List<Freelancer>>(content);
        return freels;
    }

    //TODO add checks for input correctness
    static internal async Task AddRssUrlAsync(string messageText, long chatId)
    {
        string path = "freelancers.json";
        var tg = new Telegram();

        if (!(File.Exists(path)))
        {
            await tg.SendMessageAsync(chatId, "DB doesn't exist 😥\nUse \"/start\" or if triend connect @FernDragonborn");
            log.Fatal("[DB]: Failed to add RssUrl: json file not exists");
        }

        //TODO check if works normally (especially RssUrl)
        string RssName = messageText.TrimStart().Substring(11);
        int SpaceIndex = RssName.IndexOf(" ");
        RssName = RssName.Remove(SpaceIndex, RssName.Length - SpaceIndex);
        string RssUrl = messageText.TrimStart().Substring(11 + RssName.Length);

        string content = File.ReadAllText(path);
        List<Freelancer> freels = JsonConvert.DeserializeObject<List<Freelancer>>(content);
        if (freels.Count != 0)
        {
            foreach (Freelancer freel in freels)
            {
                if (freel.ChatId == chatId)
                {
                    var Rss = new string[2] { RssName, RssUrl };
                    freel.RssStrings.Add(Rss);
                    string json = JsonConvert.SerializeObject(freels);
                    File.WriteAllText(path, json);
                    log.Info($"[DB]: Added new RSS to {freel.Name}");
                    await tg.SendMessageAsync(chatId, "Added RSS succesfully 😎");
                    return;
                }
            }
        }
        else
        {
            await tg.SendMessageAsync(chatId, "DB doesn't exist 😥\nUse \"/start\" or if triend connect @FernDragonborn");
            log.Error("[DB]: Failed to add RssUrl: freelansers count in list is 0");
        }
    }

    static internal async Task CreatNewFreelancerAsync(string name, long chatId)
    {
        string path = "freelancers.json";
        var freelancer = new Freelancer(name, chatId);
        var tg = new Telegram();

        if (!(File.Exists(path)))
        {
            File.Create(path).Close();
            var freels2 = new List<Freelancer> { freelancer };
            string json2 = JsonConvert.SerializeObject(freels2);
            File.WriteAllText(path, json2);
            return;
        }

        string content = File.ReadAllText(path);
        List<Freelancer> freels = JsonConvert.DeserializeObject<List<Freelancer>>(content);
        if (freels.Count != 0)
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
        File.WriteAllText(path, json);
        await tg.SendMessageAsync(chatId, "Succesfully created your profile 😊");
        log.Info("[DB]: Created new freelancer");
    }

    internal List<Post> getPosts()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.Load(dataPath);
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
        return PostList;
    }
}

