namespace UpWorkTgBot;

internal class Program
{
    static async Task Main()
    {
        var tg = new Telegram();
        tg.init();
        if (!(File.Exists("freelancers.json")))
        {
            await DB.CreatNewFreelancerAsync("test", 0);
        }
        List<Freelancer> freelList = DB.GetFreelancers();
        int iterations = 0;
        var a = new List<string>();
        while (true)
        {
            Thread.Sleep(10 * 60 * 1000); //first number is for minutes between new posts

            foreach (Freelancer freel in freelList)
            {
                foreach (string rssUrl in freel.RssStrings)
                {
                    var db = new DB(rssUrl);
                    //if (!(File.Exists("data.xml")))
                    db.downloadData();
                    var Postlist = new List<Post>(db.getPosts());
                    foreach (Post post in Postlist)
                    {
                        if (a.Contains(post.Title)) continue;
                        tg.SendPostAsync(freel, post);
                        a.Add(post.Title);
                    }
                }
                Console.WriteLine($"posts sended to {freel.Name}");
            }
            if (iterations >= 100)
            {
                iterations = 0;
                a.Clear();
            }

            iterations++;
        }
    }
}

