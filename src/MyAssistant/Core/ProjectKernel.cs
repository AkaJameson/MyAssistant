using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using MyAssistant.Data.ProjectGenerater;
using MyAssistant.Plugins;
using System.Text.Json;

namespace MyAssistant.Core
{
    public class ProjectKernel
    {
        private readonly Kernel _kernel;

        public ProjectKernel(
            Models.ModelConfig modelConfig)
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(
                modelConfig.Model,
                new Uri(modelConfig.Endpoint),
                modelConfig.ApiKey
            );

            _kernel = builder.Build();

            _kernel.Plugins.AddFromObject(new ProjectPromptPlugin());
            _kernel.Plugins.AddFromObject(new ProjectPlugin(new ProjectPromptPlugin()));
        }

        public async Task<string> GenerateProjectStructure(
         string projectName,
         string description,
         string techStack = "")
        {
            // 创建执行参数
            var arguments = new KernelArguments
        {
            {"kernel",_kernel },
            { "description", description },
            { "projectName", projectName },
            { "techStack", techStack }
        };

            // 调用 ProjectPlugin 的 CreateProject 方法 - 这才是使用插件！
            var result = await _kernel.InvokeAsync<string>(
                "ProjectPlugin",
                "CreateProject",
                arguments);

            return result;
        }
    }

}
