using LiteDB;

namespace MyAssistant.IServices
{
    public interface IAgentService
    {
        Task BuildPromptAsync(string sessionId, string knowledgeSetId);
        Task<(string ZipBase64, bool Success, List<string> Errors)> CreateProjectFileAsync(string sessionId,CancellationToken cancellationToken =default);
    }
}
