using LiteDB;
using MyAssistant.Data;

namespace MyAssistant.Repository
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


        public IEnumerable<ChatSession> GetAllSummery()
        {
            var allSessions = _collection.Query().ToList();

            return allSessions.Select(session => new ChatSession
            {
                SessionId = session.SessionId,
                CreatedAt = session.CreatedAt,
                LastUpdatedAt = session.LastUpdatedAt,
                Id = session.Id,
                Messages = session.Messages?.Take(1).Select(msg => new ChatMessage
                {
                    Event = msg.Event,
                    AssistantResponse = msg.AssistantResponse?.Length > 50
                        ? msg.AssistantResponse.Substring(0, 50) + "..."
                        : msg.AssistantResponse,
                    UserInput = msg.UserInput?.Length > 50
                        ? msg.UserInput.Substring(0, 50) + "..."
                        : msg.UserInput,
                    Round = msg.Round,
                    Timestamp = msg.Timestamp
                }).ToList() ?? new List<ChatMessage>()
            }).ToList();
        }
    }
}
