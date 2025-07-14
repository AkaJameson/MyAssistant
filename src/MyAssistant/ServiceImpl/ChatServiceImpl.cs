using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using MyAssistant.IServices;
using Microsoft.AspNetCore.SignalR;
using MyAssistant.Hubs;
using MyAssistant.Models;

namespace MyAssistant.ServiceImpl
{
    public class ChatServiceImpl : IChatService
    {
        private static Kernel _currentKernel;
        private static string _currentModelName;
        private readonly IHubContext<ChatHub> hubContext;
        private readonly IConfiguration configuration;
        public ChatServiceImpl(IHubContext<ChatHub> hubContext, IConfiguration configuration)
        {
            this.hubContext = hubContext;
            this.configuration = configuration;
        }
        /// <summary>
        /// 获取模型配置Json
        /// </summary>
        /// <returns></returns>
        public string GetModelConfigs()
        {
            return configuration.GetSection("ModelConfigs").Get<string>() ?? "";
        }
        private Task UpdateKernel(string modelName)
        {
            if (_currentModelName != null || _currentModelName == modelName)
            {
                return Task.CompletedTask;
            }
            configuration.GetSection("ModelConfigs").Get<string>();
            var modelConfigs = configuration.GetSection("ModelConfigs").Get<List<ModelConfig>>();
            if (modelConfigs == null)
            {
                return Task.CompletedTask;
            }
            var modelConfig = modelConfigs.FirstOrDefault(x => x.Model == modelName);
            if (modelConfig == null)
            {
                return Task.CompletedTask;
            }

        }
        public async Task StartChatSession(Kernel kernel, string input)
        {
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();

            while (true)
            {

                history.AddUserMessage(input);
                var response = await chatService.GetChatMessageContentAsync(history);
                await hubContext.Clients.All.SendAsync(EventType.ChatMessage, response.Content); //发送聊天记录到客户端
                history.AddAssistantMessage(response.Content);
            }
        }

        public async Task StartStreamingChatSession(Kernel kernel, string input)
        {
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();

            while (true)
            {

                history.AddUserMessage(input);

                var response = chatService.GetStreamingChatMessageContentsAsync(
                    chatHistory: history,
                    kernel: kernel
                );
                string resStr = "";
                await foreach (var chunk in response)
                {
                    await hubContext.Clients.All.SendAsync(EventType.ChatMessage, chunk); //发送聊天记录到客户端
                    resStr += chunk;

                }
                history.AddAssistantMessage(resStr);
            }
        }
    }
}
