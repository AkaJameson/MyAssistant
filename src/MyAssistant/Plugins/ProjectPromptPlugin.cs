// ProjectPromptPlugin.cs
using System.Text.Json;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MyAssistant.Data.ProjectGenerater;

namespace MyAssistant.Plugins
{
    public class ProjectPromptPlugin
    {
        public ProjectPromptPlugin()
        {
        }

        [KernelFunction("GenerateProjectStructure")]
        [Description("根据项目描述生成项目结构JSON")]
        public async Task<string> GenerateProjectStructure(
            Kernel kernel,
            [Description("项目名称")] string projectName,
            [Description("项目描述")] string description,
            [Description("技术栈")] string techStack = "")
        {
            // 构建系统提示
            var systemPrompt = BuildSystemPrompt(projectName, techStack);
            
            // 创建新的聊天历史
            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(description);
            
            // 获取聊天服务
            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
            var settings = new OpenAIPromptExecutionSettings 
            { 
                ResponseFormat = "json_object",
                MaxTokens = 4000 
            };
            
            // 调用AI生成结构
            var response = await chatCompletion.GetChatMessageContentAsync(
                chatHistory, settings, kernel);
            
            // 验证和返回JSON
            return ValidateAndFormatJson(response.Content);
        }

        private string BuildSystemPrompt(string projectName, string techStack)
        {
            var structureSchema = JsonSerializer.Serialize(new ProjectStructure(), new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var exampleJson = """
                {
                  "ProjectName": "ConsoleApp",
                  "Items": [
                    {
                      "type": "File",
                      "path": "Program.cs",
                      "content": "using System;\n\nnamespace ConsoleApp\n{\n    class Program\n    {\n        static void Main(string[] args)\n        {\n            Console.WriteLine(\"Hello World!\");\n        }\n    }\n}",
                      "isEntryPoint": true
                    },
                    {
                      "type": "File",
                      "path": "ConsoleApp.csproj",
                      "content": "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net8.0</TargetFramework>\n  </PropertyGroup>\n</Project>"
                    }
                  ]
                }
                """;

                        return $"""
                ## 角色
                你是一个专业的项目结构生成器，需要根据用户需求创建完整的项目结构。

                ## 任务
                生成一个名为 "{projectName}" 的项目结构，技术栈: {techStack}

                ## 输出要求
                1. 必须使用严格的 JSON 格式输出
                2. 使用以下数据结构:
                   {structureSchema}
                3. 对于目录，设置 "type": "Directory", "content" 为空
                4. 对于文件，提供完整的文件内容
                5. 标记入口文件为 "isEntryPoint": true
                6. 使用正斜杠(/)作为路径分隔符
                7. 包含所有必要的配置文件

                ## 示例
                对于 C# 控制台项目:
                {exampleJson}
                """;
        }


        private string ValidateAndFormatJson(string json)
        {
            try
            {
                // 验证JSON格式
                var project = JsonSerializer.Deserialize<ProjectStructure>(json);
                
                // 修复常见问题
                json = json.Replace("\\n", "\n")
                           .Replace("\\t", "\t");
                
                return json;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("AI返回了无效的项目结构格式");
            }
        }
    }
}
