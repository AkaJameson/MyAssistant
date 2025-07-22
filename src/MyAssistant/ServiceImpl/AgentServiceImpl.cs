using LiteDB;
using MyAssistant.Core;
using MyAssistant.IServices;
using System.Text;

namespace MyAssistant.ServiceImpl
{
    public class AgentServiceImpl : IAgentService
    {
        private readonly IKnowledgeService _knowledgeService;
        private readonly ChatContext _chatContext;
        private readonly ILogger<AgentServiceImpl> _logger;

        public AgentServiceImpl(
            IKnowledgeService knowledgeService,
            ChatContext chatContext,
            ILogger<AgentServiceImpl> logger)
        {
            _knowledgeService = knowledgeService;
            _chatContext = chatContext;
            _logger = logger;
        }

        public async Task BuildPromptAsync(string sessionId, string knowledgeSetId)
        {
            if (string.IsNullOrEmpty(knowledgeSetId))
                throw new ArgumentException("Invalid knowledge set ID");

            var set = await _knowledgeService.GetKnowledgeSetByIdAsync(knowledgeSetId);
            if (set == null) throw new Exception("Knowledge set not found");

            var files = await _knowledgeService.GetKnowledgeFilesBySetIdAsync(knowledgeSetId);
            if (files.Count == 0) throw new Exception("No files in knowledge set");

            var sb = new StringBuilder(set.PromptTemplate);
            foreach (var file in files)
            {
                sb.Replace($"{{{{{file.Title}}}}}", file.Content);
            }

            _chatContext.AddSystemMessage(sessionId, sb.ToString());
            _logger.LogInformation($"Loaded knowledge set {set.Name} into session {sessionId}");
        }
    }
}
