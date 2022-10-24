using System.Net;
using System.Xml;

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
        using (var client = new WebClient())
        {
            client.DownloadFile(url, dataPath);
            //source = client.DownloadString(url);
        }
    }
    static internal void CreatNewFreelancer(string name, long chatId)
    {
        var freel = new Freelancer(name, chatId);
        var freelancersXml = new XmlDocument();
        if (!(File.Exists("freelancers.xml"))) CreateFreelancersXml();
        freelancersXml.LoadXml("freelancers.xml");
        XmlElement? xRoot = freelancersXml.DocumentElement;
        XmlNode xNode = freelancersXml.CreateNode(XmlNodeType.Element, name, "");
        freelancersXml.Save("freelancers.xml");
    }
    static private void CreateFreelancersXml()
    {
        File.Create("freelancers.xml");
        var freelancersXml = new XmlDocument();
        freelancersXml.CreateElement("freeelancers");
        freelancersXml.Save("freelancers.xml");
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
}

