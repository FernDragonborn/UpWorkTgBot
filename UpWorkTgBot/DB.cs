using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
    string dataPath = "data.xml";
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

    static internal async Task AddRssUrl(string RssUrl, long chatId)
    {
        var tg = new Telegram();
        string path = "freelancers.json";
        bool isDone = false;

        string source = File.ReadAllText(path);
        var sr = new StreamReader(path);
        var reader = new JsonTextReader(sr);
        sr.Close();
        var sw = new StreamWriter(path);
        var writer = new JsonTextWriter(sw);
        sw.Close();
        JsonSerializer serializer = new JsonSerializer();
        serializer.NullValueHandling = NullValueHandling.Ignore;
        serializer.Converters.Add(new JavaScriptDateTimeConverter());

        var freelList = JsonConvert.DeserializeObject<List<Freelancer>>(source);
        foreach (Freelancer freel in freelList)
        {
            if (freel.ChatId == chatId)
            {
                freel.RssStrings.Add(RssUrl);
                serializer.Serialize(writer, freel);
                Console.WriteLine($"{DateTime.Now}\t[DB]: Added new RSS to {freel.Name}");
                tg.SendMessageAsync(chatId, "Added RSS succesfully");
                isDone = true;
            }
        }
        if (isDone == false)
        {
            Console.WriteLine($"{DateTime.Now}\t[DB]: Failed to add RSS");
            tg.SendMessageAsync(chatId, "Your RSS haven't been added, connect with Fern");
        }

    }

    static internal async Task CreatNewFreelancerAsync(string name, long chatId)
    {
        string path = "freelancers.json";
        var freelancer = new Freelancer(name, chatId);

        JsonSerializer serializer = new JsonSerializer();

        serializer.NullValueHandling = NullValueHandling.Ignore;
        serializer.Converters.Add(new JavaScriptDateTimeConverter());

        using (StreamWriter sw = new StreamWriter(path))
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            serializer.Serialize(writer, freelancer);
        }
        Console.WriteLine($"{DateTime.Now}\t[DB]: Created new freelancer");
    }

    internal List<Post> getPosts()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.Load(dataPath);
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
        return PostList;
    }
    static private void CreatefreelDoc()
    {
        File.Create("freelancers.json").Close();
    }
}

