using LiteDB;

namespace MyAssistant.IServices
{
    public interface IAgentService
    {
        Task BuildPromptAsync(string sessionId, string knowledgeSetId);
    }
}
