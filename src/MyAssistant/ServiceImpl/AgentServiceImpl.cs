using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using MyAssistant.Core;
using MyAssistant.IServices;
using System.Text;
using MyAssistant.Utils;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace MyAssistant.ServiceImpl
{
    public class AgentServiceImpl : IAgentService
    {
        private readonly IKnowledgeService _knowledgeService;
        private readonly ChatContext _chatContext;
        private readonly ILogger<AgentServiceImpl> _logger;
        private FunctionBasedProjectService _projectService;
        public AgentServiceImpl(
            IKnowledgeService knowledgeService,
            ChatContext chatContext,
            ILogger<AgentServiceImpl> logger,
            FunctionBasedProjectService projectService)
        {
            _knowledgeService = knowledgeService;
            _chatContext = chatContext;
            _logger = logger;
            _projectService = projectService;
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

        public async Task<(string ZipBase64, bool Success, List<string> Errors)> CreateProjectFileAsync(string sessionId)
        {
            try
            {
                var history = _chatContext.GetOrCreateChatHistory(sessionId);
                var userHistory = string.Join("\n", history
                    .Where(m => m.Role != AuthorRole.System)
                    .TakeLast(Math.Min(history.Count, 10)) 
                    .Select(m => $"{m.Role}: {m.Content}"));

                var result = await _projectService.CreateProjectAsync(userHistory);
                return result;
             
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建项目文件时出错");
                return (string.Empty, false, new List<string> { $"服务错误: {ex.Message}" });
            }
        }
    }
}
