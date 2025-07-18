using Microsoft.SemanticKernel.ChatCompletion;
using MyAssistant.Data;
using System.Collections.Concurrent;

namespace MyAssistant.Core
{
    public class ChatContext
    {
        private readonly ConcurrentDictionary<string, (ChatHistory History, DateTime LastActive)> _chatHistories = new();

        public ChatHistory GetOrCreateChatHistory(string sessionId, ChatSession chatSession = null)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException("会话 ID 不能为空。");
            }

            var result = _chatHistories.GetOrAdd(sessionId, key =>
            {
                var history = new ChatHistory();

                // 添加系统提示
                history.AddSystemMessage("首要要求：- 所有输出请使用 Markdown 格式");

                if (chatSession != null)
                {
                    foreach (var msg in chatSession.Messages)
                    {
                        if (!string.IsNullOrWhiteSpace(msg.UserInput))
                            history.AddUserMessage(msg.UserInput);
                        if (!string.IsNullOrWhiteSpace(msg.AssistantResponse))
                            history.AddAssistantMessage(msg.AssistantResponse);
                    }
                }

                return (history, DateTime.UtcNow);
            });

            UpdateLastActive(sessionId);
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