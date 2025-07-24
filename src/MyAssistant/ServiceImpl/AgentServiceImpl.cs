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

        public async Task<(string ZipBase64, bool Success, List<string> Errors)> CreateProjectFileAsync(string sessionId)
        {
            try
            {
                var history = _chatContext.GetOrCreateChatHistory(sessionId);
                var userHistory = string.Join("\n", history
                    .Where(m => m.Role != AuthorRole.System)
                    .TakeLast(Math.Min(history.Count, 10)) 
                    .Select(m => $"{m.Role}: {m.Content}"));

                string projectPrompt = UniversalProjectGenerator.GeneratePrompt(userHistory);

                var tempHistory = new ChatHistory();
                tempHistory.AddSystemMessage(projectPrompt);
                tempHistory.AddUserMessage(userHistory);

                var chatCompletion = _kernelContext.Current.GetRequiredService<IChatCompletionService>();

                var settings = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.1f,
                    TopP = 0.9f,
                    MaxTokens = 4000, 
                    StopSequences = new List<string> { "## /END" }
                };

                var result = await chatCompletion.GetChatMessageContentsAsync(
                    tempHistory,
                    settings,
                    cancellationToken: default
                );

                var markdown = string.Join("\n", result.Select(m => m.Content));

                var (zipBase64, success, errors) = ProjectParser.ParseAndCreateProject(markdown);

                if (success)
                {
                    _logger.LogInformation("项目生成成功并打包返回");
                    return (zipBase64, true, new List<string>());
                }
                else
                {
                    _logger.LogWarning("项目解析失败: {Errors}", string.Join(", ", errors));
                    return (string.Empty, false, errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建项目文件时出错");
                return (string.Empty, false, new List<string> { $"服务错误: {ex.Message}" });
            }
        }
    }
}
