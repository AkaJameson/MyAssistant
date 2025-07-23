using LiteDB;
using Microsoft.SemanticKernel;
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
        private readonly KernelContext _kernelContext;

        public AgentServiceImpl(
            IKnowledgeService knowledgeService,
            ChatContext chatContext,
            ILogger<AgentServiceImpl> logger,
            KernelContext kernelContext)
        {
            _knowledgeService = knowledgeService;
            _chatContext = chatContext;
            _logger = logger;
            _kernelContext = kernelContext;
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

        public async Task<string> GenerateProject(
           string sessionId,
           string description,
           string projectName,
           string techStack = "")
        {
            try
            {
                // 使用专用项目内核生成项目
                var zipBase64 = await _kernelContext.ProjectKernel.GenerateProjectStructure(
                    projectName, description, techStack);
                return zipBase64;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "项目生成失败");
                return null;
            }
        }
    }
}
