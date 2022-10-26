using System.Text;
using File = System.IO.File;

namespace UpWorkTgBot;

internal class Program
{
    static async Task Main()
    {
        var tg = new Telegram();
        tg.init();

        string url = "https://www.upwork.com/ab/feed/jobs/rss?api_params=1&amp;budget=100-499%2C500-999%2C1000-4999%2C5000-&amp;job_type=hourly%2Cfixed&amp;ontology_skill_uid=1031626756493656064&amp;orgUid=1526554921826770945&amp;paging=0%3B10&amp;proposals=0-4%2C5-9&amp;q=&amp;securityToken=be3fbd85f54c1b3c626d21e8815a06b4fd21b61c4dc2f2664ce0109112f6c4a9f3e3504d635eba812bb1d38e21af9fe2661570c91ad8e396abdd056cfbc91559&amp;sort=recency&amp;userUid=1526554921826770944&amp;verified_payment_only=1&amp;workload=as_needed%2Cpart_time";
        var db = new DB(url);
        if (!(File.Exists("data.xml"))) db.downloadData();
        var PostList = new List<Post>(db.getPosts());

        var freelancer = new Freelancer("Fern", 561838359);

        var post = new Post()
        {
            Title = "IMMEDIATE HIRE: Develop comment form with JS and store in array and display - Upwork",
            Description = "I have a web application on Microsoft forms on visual code on c#.<br />\nIt is connected to localDB and working fine. I want to add menu with short cut but I get format issue and can&#039;t fix format.<br /><br /><b>Budget</b>: $10\n<br /><b>Posted On</b>: October 23, 2022 18:37 UTC<br /><b>Category</b>: Desktop Software Development<br /><b>Skills</b>:Microsoft Visual Studio,     Application Improvement,     Microsoft Windows,     .NET Framework,     C#,     Microsoft SQL Server    \n<br /><b>Country</b>: United Arab Emirates\n<br /><a href=\"https://www.upwork.com/jobs/Fixing-visual-stuido-microsoft-form_%7E019312cd7ce7c87403?source=rss\">click to apply</a>\n",
            PubDate = "Sun, 23 Oct 2022 12:15:58 +0000"
        };

        var sb = new StringBuilder();
        sb.Append($"<b>Title: </b>\n{post.Title}\n");
        sb.Append($"<b>Description: </b>\n{post.Description}\n");
        sb.Append($"<b>Publicated: </b>\n{post.PubDate}");
        sb.Replace("<br />", "\n");
        //await tg.SendMessageAsync(freelancer, sb.ToString());
        await DB.CreatNewFreelancerAsyncNew("Fern", 561838359);

        Console.ReadKey();



    }
}

