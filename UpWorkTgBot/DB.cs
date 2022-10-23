using System.Net;

namespace UpWork_bot;

internal class DB
{
    public DB(string url)
    {
        _ = url;
    }
    internal string url { get; set; }
    internal void downloadData()
    {
        using (var client = new WebClient())
        {
            client.DownloadFile(url, "data.xml");
            //source = client.DownloadString(url);
        }
    }
}

