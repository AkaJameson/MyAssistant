using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MyAssistant.Core;
using MyAssistant.Utils;
using System.Text;

namespace MyAssistant.ServiceImpl
{
    /// <summary>
    /// 项目文件定义
    /// </summary>
    public class ProjectFile
    {
        public string Path { get; set; } = "";
        public string Content { get; set; } = "";
        public string FileType { get; set; } = "text";
    }

    /// <summary>
    /// 改进的项目生成服务
    /// </summary>
    public class FunctionBasedProjectService
    {
        private readonly ILogger<FunctionBasedProjectService> _logger;
        private readonly KernelContext kernelContext;
        public FunctionBasedProjectService(
            ILogger<FunctionBasedProjectService> logger, KernelContext kernelContext)
        {
            _logger = logger;
            this.kernelContext = kernelContext;
        }

        public async Task<(string ZipBase64, bool Success, List<string> Errors)> CreateProjectAsync(
            string userHistory,
            CancellationToken cancellationToken = default)
        {
            var projectBuilder = new ProjectBuilder();
            var errors = new List<string>();

            try
            {
                // 分析项目上下文
                var context = UniversalProjectGenerator.AnalyzeProjectContext(userHistory);

                // 构建系统提示
                var systemPrompt = BuildSystemPrompt(context);

                // 创建对话历史
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage($"根据以下对话历史创建项目：\n\n{userHistory}");

                // 启用函数调用的设置
                var executionSettings = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.1f,
                    MaxTokens = 4000,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                };
                var _chatService = kernelContext.Current.GetRequiredService<IChatCompletionService>();
                // 执行对话，AI会自动调用函数
                var result = await _chatService.GetChatMessageContentsAsync(
                    chatHistory,
                    executionSettings,
                    kernelContext.Current,
                    cancellationToken);

                // 检查是否有文件创建
                var files = projectBuilder.GetFiles();
                if (files.Count == 0)
                {
                    // 如果没有文件，尝试引导AI创建
                    chatHistory.AddAssistantMessage(string.Join("\n", result.Select(r => r.Content)));
                    chatHistory.AddUserMessage("请使用CreateFile函数创建项目中的所有必要文件。确保调用InitializeProject开始，然后逐个创建每个文件，最后调用FinalizeProject完成。");

                    await _chatService.GetChatMessageContentsAsync(
                        chatHistory,
                        executionSettings,
                        kernelContext.Current,
                        cancellationToken);

                    files = projectBuilder.GetFiles();
                }

                if (files.Count == 0)
                {
                    return ("", false, new List<string> { "AI未能创建任何文件，请检查提示词或重试" });
                }

                // 导出为ZIP
                var (zipBase64, success, zipErrors) = projectBuilder.ExportToZip();

                if (success)
                {
                    _logger.LogInformation("通过函数调用成功创建项目，共 {FileCount} 个文件", files.Count);
                }

                return (zipBase64, success, zipErrors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "函数调用方式创建项目失败");
                return ("", false, new List<string> { $"创建项目失败: {ex.Message}" });
            }
        }

        private string BuildSystemPrompt(ProjectContext context)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("你是一名专业的软件开发工程师。你需要根据用户的对话历史创建一个完整的项目。");
            prompt.AppendLine();
            prompt.AppendLine("**你必须使用以下函数来创建项目：**");
            prompt.AppendLine("1. InitializeProject(projectName, description) - 初始化项目");
            prompt.AppendLine("2. CreateFile(filePath, content, fileType) - 创建文件");
            prompt.AppendLine("3. FinalizeProject() - 完成项目");
            prompt.AppendLine();
            prompt.AppendLine("**工作流程：**");
            prompt.AppendLine("1. 首先调用 InitializeProject 设置项目信息");
            prompt.AppendLine("2. 逐个调用 CreateFile 创建所有必要的文件");
            prompt.AppendLine("3. 最后调用 FinalizeProject 完成项目");
            prompt.AppendLine();

            if (!string.IsNullOrEmpty(context.Language))
            {
                prompt.AppendLine($"**检测到的编程语言：** {context.Language}");
            }
            if (!string.IsNullOrEmpty(context.Framework))
            {
                prompt.AppendLine($"**检测到的技术框架：** {context.Framework}");
            }
            if (!string.IsNullOrEmpty(context.ProjectType))
            {
                prompt.AppendLine($"**检测到的项目类型：** {context.ProjectType}");
            }

            prompt.AppendLine();
            prompt.AppendLine("**重要要求：**");
            prompt.AppendLine("- 文件路径使用相对路径，不要以/开头");
            prompt.AppendLine("- 代码必须完整可运行，包含所有必要的导入");
            prompt.AppendLine("- 遵循语言的最佳实践和编码规范");
            prompt.AppendLine("- 包含项目配置文件（如.csproj, package.json等）");
            prompt.AppendLine("- 包含README.md文档");

            return prompt.ToString();
        }
    }
}
