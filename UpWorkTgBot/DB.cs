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

    internal async Task AddRssUrl(long chatId)
    {
        string path = "freelancers.xml";

        XmlTextReader reader = new XmlTextReader(path);

        XmlDocument doc = new XmlDocument();
        XmlNode node = doc.ReadNode(reader);

        var freelancer = new Freelancer();
        foreach (XmlNode chldNode in node.ChildNodes)
        {
            //Read the attribute Name
            if (chldNode.Name == "freelancer" && chldNode.HasChildNodes)
            {
                foreach (XmlNode item in node.ChildNodes)
                {
                    var freelName = node.InnerText;
                    if (freelName is null) break;
                    // Process the value here
                }

            }
        }

        var freelDoc = new XmlDocument();
        freelDoc.Load(path);

        var writer = new System.Xml.Serialization.XmlSerializer(typeof(Freelancer));

        XmlElement? xRoot = freelDoc.DocumentElement;
        //XmlNode freelancerNode = freelDoc.CreateNode(XmlNodeType.Element, name, "");
        XmlNodeList xList = xRoot.GetElementsByTagName("freelancer");


        var file = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
        writer.Serialize(file, freelancer);
        file.Close();
    }

    static internal async Task CreatNewFreelancerAsync(string name, long chatId)
    {
        string path = "freelancers.json";
        var freelancer = new Freelancer(name, chatId);

        //var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        JsonSerializer serializer = new JsonSerializer();

        serializer.NullValueHandling = NullValueHandling.Ignore;
        serializer.Converters.Add(new JavaScriptDateTimeConverter());

        using (StreamWriter sw = new StreamWriter(path))
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            serializer.Serialize(writer, freelancer);
        }
        Console.WriteLine("[DB]: Created new freelancer");
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

