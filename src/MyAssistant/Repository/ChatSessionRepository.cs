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
            return _collection.Query().Select(x => new ChatSession
            {
                SessionId = x.SessionId,
                CreatedAt = x.CreatedAt,
                LastUpdatedAt = x.LastUpdatedAt,
                Id = x.Id,
                Messages = new List<ChatMessage>
                {
                    x.Messages.Select(y => new ChatMessage
                    {
                        Event = y.Event,
                        AssistantResponse = y.AssistantResponse.Length > 50 ? y.AssistantResponse.Substring(0, 50) + "..." : y.AssistantResponse,
                        UserInput = y.UserInput.Length > 50 ? y.UserInput.Substring(0, 50) + "..." : y.UserInput,
                        Round = y.Round,
                        Timestamp = y.Timestamp
                    }).FirstOrDefault()?? new ChatMessage()
                }
            }).ToList();
        }


    }
}
