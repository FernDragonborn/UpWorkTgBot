using log4net;

[assembly: log4net.Config.XmlConfigurator]

namespace UpWorkTgBot;

internal class Program
{
    static readonly ILog log = LogManager.GetLogger(typeof(Program));

    static async Task Main()
    {
        Console.WriteLine("started. If no messages after, logger haven't initialized");
        log.Info("logger initialized");

        DotNetEnv.Env.Load(".env");

        string PART_FREEL_PATH = DotNetEnv.Env.GetString("FREEL_PATH");
        string FREEL_PATH = $"{Directory.GetCurrentDirectory()}{PART_FREEL_PATH}";
        if (!(File.Exists(FREEL_PATH)))
        {
            await DB.CreatNewFreelancerAsync("test", 0);
        }
        List<Freelancer> freelList = DB.GetFreelancers();
        var shownPosts = new List<string>();

        var tg = new Telegram();
        await tg.init();

        int iterations = 0;
        while (true)
        {

            foreach (Freelancer freel in freelList)
            {
                bool postBeenSended = false;
                shownPosts = freel.ShownPosts;
                if (freel.RssStrings is null) break;
                foreach (string[] rssUrl in freel.RssStrings)
                {
                    var db = new DB(rssUrl[1]);
                    //if (!(File.Exists("data.xml")))
                    db.downloadData();
                    var Postlist = new List<Post>(db.getPosts());
                    foreach (Post post in Postlist)
                    {
                        if (shownPosts.Contains(post.Title)) break;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        tg.SendPostAsync(freel, post);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        shownPosts.Add(post.Title);
                        postBeenSended = true;
                    }
                }
                if (postBeenSended && shownPosts is not null) await DB.SaveShownPosts(shownPosts, freel.ChatId);

            }
            if (iterations >= 72) //clears twice per day if iteration takes 10 min
            {
                iterations = 0;
                shownPosts.Reverse();
                shownPosts.RemoveRange(60, shownPosts.Count - 60);
            }

            iterations++;

            //TODO rewrite for real 10 minute iterations (now it's more than 10 min)
            int MINUTES = Convert.ToInt32(DotNetEnv.Env.GetString("TIME_SPAN"));
            Thread.Sleep(MINUTES * 60 * 1000); //first number is for minutes between new posts
        }
    }
}

