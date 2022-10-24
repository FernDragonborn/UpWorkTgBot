namespace UpWorkTgBot
{
    internal class Freelancer
    {
        public Freelancer(string name, long chatId)
        {
            Id = Guid.NewGuid();
            Name = name;
            ChatId = chatId;
        }
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public long ChatId { get; private set; }
    }
}
