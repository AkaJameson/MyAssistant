using MyAssistant.Data;

namespace MyAssistant.IServices
{
    public interface IKnowledgeService
    {
        Task<string> CreateKnowledgeFileAsync(string knowledgeSetId, string fileName, string content);
        Task<string> CreateKnowledgeSetAsync(string name, string template);
        Task<bool> DeleteKnowledgeFileAsync(string id);
        Task<bool> DeleteKnowledgeSetAsync(string id);
        Task<KnowledgeFile?> GetKnowledgeFileByIdAsync(string id);
        Task<List<KnowledgeFile>> GetKnowledgeFilesBySetIdAsync(string knowledgeSetId);
        Task<KnowledgeSet?> GetKnowledgeSetByIdAsync(string id);
        Task<List<KnowledgeSet>> QueryAllSets();
        Task<bool> UpdateKnowledgeFileAsync(string id, string fileName, string content);
        Task<bool> UpdateKnowledgeSetAsync(string id, string name, string template);
    }
}