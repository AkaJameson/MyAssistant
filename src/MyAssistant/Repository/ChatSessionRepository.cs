using LiteDB;

namespace MyAssistant.Data
{
    public class ChatSessionRepository
    {
        private readonly ILiteCollection<ChatSession> _collection;

        public ChatSessionRepository(LiteDatabase db)
        {
            _collection = db.GetCollection<ChatSession>("chat_sessions");
        }

        public ObjectId Insert(ChatSession session)
        {
            return _collection.Insert(session);
        }

        public bool Update(ChatSession session)
        {
            session.LastUpdatedAt = DateTime.UtcNow;
            return _collection.Update(session);
        }

        public bool Delete(ObjectId id) => _collection.Delete(id);

        public ChatSession? FindBySessionId(string sessionId)
            => _collection.FindOne(x => x.SessionId == sessionId);

        public IEnumerable<ChatSession> GetAll() => _collection.FindAll();
    }
}
