using LiteDB;

namespace MyAssistant.Data
{
    public class ChatSession
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string SessionId { get; set; } = "";  // 自定义会话ID

        public List<ChatMessage> Messages { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
