using MyAssistant.Data;

namespace MyAssistant.IServices
{
    public interface IKnowledgeService
    {
        Task<string> CreateKnowledgeSetAsync(string name, string template);
        Task<List<KnowledgeSet>> QueryAllSetsAsync();
        Task<KnowledgeSet?> GetKnowledgeSetByIdAsync(string id);
        Task<bool> UpdateKnowledgeSetAsync(string id, string name, string template);
        Task<bool> DeleteKnowledgeSetAsync(string id);
        Task<string> CreateKnowledgeFileAsync(string knowledgeSetId, string fileName, string content);
        Task<List<KnowledgeFile>> GetKnowledgeFilesBySetIdAsync(string knowledgeSetId);
        Task<KnowledgeFile?> GetKnowledgeFileByIdAsync(string id);
        Task<bool> UpdateKnowledgeFileAsync(string id, string fileName, string content);
        Task<bool> DeleteKnowledgeFileAsync(string id);
    }
}
