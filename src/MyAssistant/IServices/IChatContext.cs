using Microsoft.SemanticKernel.ChatCompletion;

namespace MyAssistant.IServices
{
    public interface IChatContext
    {
        ChatHistory GetOrCreateChatHistory(string sessionId);
        void RemoveChatHistory(string sessionId);
        void UpdateLastActive(string sessionId);
        void CleanupInactiveSessions(TimeSpan timeout);
    }
}
