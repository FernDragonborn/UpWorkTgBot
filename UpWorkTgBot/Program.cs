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
        var shownPosts = new List<string>();
        while (true)
        {
            //Thread.Sleep(10 * 60 * 1000); //first number is for minutes between new posts

            Thread.Sleep(2 * 1000); //first number is for minutes between new posts

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
                        if (shownPosts.Contains(post.Title)) continue;
                        tg.SendPostAsync(freel, post);
                        shownPosts.Add(post.Title);
                    }
                }
                await DB.SaveShownPosts(shownPosts, freel.ChatId);
                Console.WriteLine($"posts sended to {freel.Name}");
            }
            if (iterations >= 72) //clears twice per day if iteration takes 10 min
            {
                iterations = 0;
                shownPosts.Reverse();
                shownPosts.RemoveRange(30, shownPosts.Count - 30);
            }

            iterations++;
        }
    }
}

