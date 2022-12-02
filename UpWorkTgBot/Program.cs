[assembly: log4net.Config.XmlConfigurator]

namespace UpWorkTgBot;
//TODO add logging ang logging sending to Fern in Tg
internal class Program
{
    private static readonly log4net.ILog log = LogHelper.GetLogger();
    //private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    //private static readonly ILog log = LogManager.GetLogger(typeof(Program));

    static async Task Main()
    {
        log.Info("logger initialized");
        DotNetEnv.Env.Load();
        //DotNetEnv.Env.TraversePath().Load();
        var tg = new Telegram();
        tg.init();
        if (!(File.Exists("freelancers.json")))
        {
            await DB.CreatNewFreelancerAsync("test", 0);
        }
        List<Freelancer> freelList = DB.GetFreelancers();
        int iterations = 0;
        var shownPosts = new List<string>();

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
                if (postBeenSended) await DB.SaveShownPosts(shownPosts, freel.ChatId);

            }
            if (iterations >= 72) //clears twice per day if iteration takes 10 min
            {
                iterations = 0;
                shownPosts.Reverse();
                shownPosts.RemoveRange(60, shownPosts.Count - 60);
            }

            iterations++;

            //TODO rewrite for real 10 minute iterations (now it's more than 10 min)
            Thread.Sleep(10 * 60 * 1000); //first number is for minutes between new posts
            //Thread.Sleep(60 * 1000); 
        }
    }
}

