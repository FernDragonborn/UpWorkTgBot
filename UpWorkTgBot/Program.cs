﻿#define DEBUG 

using log4net;

[assembly: log4net.Config.XmlConfigurator]

namespace UpWorkTgBot;

internal class Program
{
    static readonly ILog log = LogManager.GetLogger(typeof(Program));

    static async Task Main()
    {
        Console.WriteLine("UpWork Post: version 24.01.2023");
        Console.WriteLine("started. If no messages after, logger haven't initialized");
        log.Info("logger initialized");
        Console.WriteLine($"is debug enabled: {log.IsDebugEnabled}");
        Console.WriteLine($"is info enabled: {log.IsInfoEnabled}");
        Console.WriteLine($"is warn enabled: {log.IsWarnEnabled}");
        Console.WriteLine($"is error enabled: {log.IsErrorEnabled}");
        Console.WriteLine($"is fatal enabled: {log.IsFatalEnabled}");

        string dotEnv = File.ReadAllText(".env");
        DotNetEnv.Env.Load(".env");

        string PART_FREEL_PATH = DotNetEnv.Env.GetString("FREEL_PATH");
        string FREEL_PATH = $"{Directory.GetCurrentDirectory()}{PART_FREEL_PATH}";
        int MAXIMUM_DAYS_SPAN_ALLOWED = Convert.ToInt32(DotNetEnv.Env.GetString("MAXIMUM_DAYS_SPAN_ALLOWED"));
        if (!(File.Exists(FREEL_PATH)))
        {
            await DB.CreatNewFreelancerAsync("test", 0);
        }
        List<Freelancer> freelList = DB.GetFreelancers();
        var shownPosts = new List<string>();

        var tg = new Telegram();
        await tg.Init();

        int iterations = 0;
        while (true)
        {
            DateTime allowedDate = DateTime.Now.AddDays(-MAXIMUM_DAYS_SPAN_ALLOWED);
            foreach (Freelancer freel in freelList)
            {
                bool postBeenSended = false;
                shownPosts = freel.ShownPosts;
#if DEBUG
                shownPosts.Clear();
                log.Debug("Cleared shown posts");
#endif
                if (freel.RssStrings is null) break;
                foreach (string[] rssUrl in freel.RssStrings)
                {
                    var db = new DB(rssUrl[1]);
                    db.downloadData();
                    var Postlist = new List<Post>(db.GetPosts());
                    foreach (Post post in Postlist)
                    {
                        if (shownPosts.Contains(post.Title))
                        {
                            log.Debug($"contains title:\t{post.Title}");
                            continue;
                        }
                        if (Convert.ToDateTime(post.PubDate) < allowedDate)
                        {
                            log.Debug($"Too old post:\t{post.Title}");
                            continue;
                        }
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        tg.SendPostAsync(freel, post);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        shownPosts.Add(post.Title);
                        postBeenSended = true;
                    }
                    log.Debug("-------- 1 LINK ITERATION --------");
                }
                if (postBeenSended && shownPosts is not null) await DB.SaveShownPosts(shownPosts, freel.ChatId);
                log.Debug("-------- 1 FREELANCER ITERATION --------");
            }
            if (iterations >= 72 && shownPosts.Count > 60) //clears twice per day if iteration takes 10 min
            {
                iterations = 0;
                shownPosts.Reverse();
                shownPosts.RemoveRange(60, shownPosts.Count - 60);
            }
            iterations++;



            //TODO rewrite for real 10 minute iterations (now it's more than 10 min)
            float MINUTES = float.Parse(DotNetEnv.Env.GetString("TIME_SPAN"));
            Thread.Sleep((int)(MINUTES * 60 * 1000)); //first number is for minutes between new posts
        }
    }
}

