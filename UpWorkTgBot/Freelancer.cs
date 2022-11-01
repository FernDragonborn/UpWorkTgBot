namespace UpWorkTgBot
{
    public class Freelancer
    {
        public Freelancer() { }
        public Freelancer(string name, long chatId)
        {
            Id = Guid.NewGuid();
            Name = name;
            ChatId = chatId;
            RssStrings = new List<string>();
        }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public long ChatId { get; set; }
        public List<string> RssStrings { get; set; }
    }
}
