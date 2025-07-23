using LiteDB;

namespace MyAssistant.IServices
{
    public interface IAgentService
    {
        Task BuildPromptAsync(string sessionId, string knowledgeSetId);
        Task<string> GenerateProject(
       string sessionId,
       string description,
       string projectName,
       string techStack = "");
    }
}
