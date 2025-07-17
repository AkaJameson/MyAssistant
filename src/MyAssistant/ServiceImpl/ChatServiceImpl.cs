using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MyAssistant.Core;
using MyAssistant.Data;
using MyAssistant.Hubs;
using MyAssistant.IServices;
using MyAssistant.Models;
using MyAssistant.Repository;
using MyAssistant.Utils;
using System.Text;
using System.Text.Json;

namespace MyAssistant.ServiceImpl
{
    public class ChatServiceImpl : IChatService
    {
        private readonly IHubContext<ChatHub> hubContext;
        private readonly IConfiguration configuration;
        private readonly ChatSessionRepository chatSessionRepository;
        private readonly ChatContext chatContext;
        private readonly KernelContext kernelManager;
        private string _currentModelName;

        public ChatServiceImpl(
            IHubContext<ChatHub> hubContext,
            IConfiguration configuration,
            ChatSessionRepository chatSessionRepository,
            ChatContext chatContext,
            KernelContext kernel)
        {
            this.hubContext = hubContext;
            this.configuration = configuration;
            this.chatSessionRepository = chatSessionRepository;
            this.chatContext = chatContext;
            this.kernelManager = kernel;
        }

        public string GetModelConfigs()
        {
            try
            {
                var modelConfigs = configuration.GetSection("ModelConfigs").Get<List<ModelConfig>>();
                if (modelConfigs == null || !modelConfigs.Any())
                {
                    return "[]";
                }
                if (modelConfigs.Any(config => !config.IsValid()))
                {
                    throw new InvalidOperationException("检测到无效的模型配置，请检查 Model、Endpoint 和 ApiKey。");
                }
                return JsonSerializer.Serialize(modelConfigs, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"无法解析 ModelConfigs 配置：{ex.Message}");
            }
        }

        public Task UpdateKernel(string modelName)
        {
            if (_currentModelName == modelName)
            {
                return Task.CompletedTask;
            }

            try
            {
                var modelConfigs = configuration.GetSection("ModelConfigs").Get<List<ModelConfig>>();
                if (modelConfigs == null || !modelConfigs.Any())
                {
                    throw new InvalidOperationException("未找到模型配置。");
                }

                var modelConfig = modelConfigs.FirstOrDefault(x => x.Model == modelName);
                if (modelConfig == null || !modelConfig.IsValid())
                {
                    throw new InvalidOperationException($"无效的模型配置：{modelName}");
                }
                kernelManager.BuildKernelByModel(modelName);
                _currentModelName = modelName;
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"更新内核失败：{ex.Message}");
            }
        }

        public async Task StartStreamingChatSession(string sessionId, string input, int contextLength = 20, params IBrowserFile[] browserFile)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("会话 ID 和输入不能为空。");
            }
            if (browserFile != null && browserFile.Length > 0)
            {
                var documentString = new StringBuilder();
                foreach (var file in browserFile)
                {
                    if (file.Size > 10 * 1024 * 1024 || !DocumentHelper.IsSupportedFileType(file.Name))
                    {
                        continue;
                    }
                    var fileContent = await DocumentHelper.ExtractFromFileAsync(file);
                    documentString.AppendLine($"{file.Name}: {fileContent}");
                    documentString.Append(Environment.NewLine);
                }
                input += Environment.NewLine + documentString.ToString();
            }
            var chatService = kernelManager.Current.GetRequiredService<IChatCompletionService>();
            var chatSession = chatSessionRepository.FindBySessionId(sessionId);
            var history = chatContext.GetOrCreateChatHistory(sessionId);
            try
            {
                // 同步数据库中的历史记录（仅在首次加载会话时）
                if (chatSession != null && history.Count == 0)
                {
                    var messages = contextLength > 0
                        ? chatSession.Messages.TakeLast(contextLength)
                        : chatSession.Messages;
                    foreach (var msg in messages)
                    {
                        history.AddUserMessage(msg.UserInput);
                        history.AddAssistantMessage(msg.AssistantResponse);
                    }
                }
                else if (chatSession == null)
                {
                    chatSession = new ChatSession
                    {
                        Id = new LiteDB.ObjectId(sessionId),
                        SessionId = sessionId,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdatedAt = DateTime.UtcNow,
                        Messages = new List<ChatMessage>()
                    };
                }

                history.AddUserMessage(input);
                var response = chatService.GetStreamingChatMessageContentsAsync(history, kernel: kernelManager.Current);
                string resStr = "";
                int round = chatSession.Messages.Count + 1;

                await foreach (var chunk in response)
                {
                    await hubContext.Clients.All.SendAsync(EventType.ChatMessage, chunk);
                    resStr += chunk;
                }

                history.AddAssistantMessage(resStr);
                chatSession.Messages.Add(new ChatMessage
                {
                    Round = round,
                    UserInput = input,
                    AssistantResponse = resStr,
                    Event = EventType.ChatMessage,
                    Timestamp = DateTime.UtcNow
                });

                // 保存会话到数据库
                if (chatSession.Id == null)
                {
                    chatSessionRepository.Insert(chatSession);
                }
                else
                {
                    chatSessionRepository.Update(chatSession);
                }

                chatContext.UpdateLastActive(sessionId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"处理聊天会话失败：{ex.Message}");
            }
        }

        public async Task UpdateModelConfigs(string newConfigsJson)
        {
            try
            {
                // 解析新的 JSON 配置
                var newConfigs = JsonSerializer.Deserialize<List<ModelConfig>>(newConfigsJson);
                if (newConfigs == null || !newConfigs.Any())
                {
                    throw new ArgumentException("新的 ModelConfigs 不能为空。");
                }

                // 验证每个配置项
                foreach (var config in newConfigs)
                {
                    if (!config.IsValid())
                    {
                        throw new ArgumentException($"无效的模型配置：{config.Model}，请检查 Model、Endpoint 和 ApiKey。");
                    }
                }

                // 更新配置
                var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                var appSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(appSettingsPath));
                if (appSettings == null)
                {
                    throw new InvalidOperationException("无法读取 appsettings.json。");
                }

                appSettings["ModelConfigs"] = newConfigs;
                await File.WriteAllTextAsync(appSettingsPath, JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true }));

                // 通知客户端配置已更新
                await hubContext.Clients.All.SendAsync(EventType.ConfigUpdated, "ModelConfigs 已更新。");
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"JSON 格式无效：{ex.Message}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"更新 ModelConfigs 失败：{ex.Message}");
            }
        }

        public void ClearSession(string sessionId)
        {
            chatContext.RemoveChatHistory(sessionId);
            var chatSession = chatSessionRepository.FindBySessionId(sessionId);
            if (chatSession != null)
            {
                chatSessionRepository.Delete(chatSession.Id);
            }
        }
        /// <summary>
        /// 获取所有会话摘要信息
        /// </summary>
        /// <returns></returns>
        public List<ChatSession> GetChatSessions()
        {
            return chatSessionRepository.GetAllSummery().ToList();
        }


    }
}