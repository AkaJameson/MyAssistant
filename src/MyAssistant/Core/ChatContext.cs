using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using MyAssistant.IServices;
using System.Collections.Concurrent;

namespace MyAssistant.Core
{
    public class ChatContext : IChatContext
    {
        private readonly ConcurrentDictionary<string, (ChatHistory History, DateTime LastActive)> _chatHistories = new();

        public ChatHistory GetOrCreateChatHistory(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException("会话 ID 不能为空。");
            }

            var result = _chatHistories.GetOrAdd(sessionId, _ => (new ChatHistory(), DateTime.UtcNow));
            UpdateLastActive(sessionId); // 更新活跃时间
            return result.History;
        }

        public void RemoveChatHistory(string sessionId)
        {
            _chatHistories.TryRemove(sessionId, out _);
        }

        public void UpdateLastActive(string sessionId)
        {
            if (_chatHistories.TryGetValue(sessionId, out var value))
            {
                _chatHistories.TryUpdate(sessionId, (value.History, DateTime.UtcNow), value);
            }
        }

        public void CleanupInactiveSessions(TimeSpan timeout)
        {
            var now = DateTime.UtcNow;
            foreach (var entry in _chatHistories)
            {
                if (now - entry.Value.LastActive > timeout)
                {
                    _chatHistories.TryRemove(entry.Key, out _);
                }
            }
        }

        public void AddSystemMessage(string sessionId, string message)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException("会话 ID 不能为空。");
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("消息内容不能为空。");
            }
            var chatHistory = GetOrCreateChatHistory(sessionId);

            foreach (var msg in chatHistory.AsEnumerable().Where(p => p.Role == AuthorRole.System).ToList())
            {
                chatHistory.Remove(msg);
            }
            chatHistory.AddSystemMessage(message);
        }

    }
}