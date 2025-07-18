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

        /// <summary>
        /// 更新内核
        /// </summary>
        /// <param name="modelName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
        /// <summary>
        /// 上传文档
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public async Task AttachFileContentToSession(string sessionId, IBrowserFile[] files)
        {
            if (files == null || files.Length == 0) return;

            var content = await ProcessUploadedFiles(files);
            if (string.IsNullOrWhiteSpace(content)) return;

            var history = chatContext.GetOrCreateChatHistory(sessionId);
            history.AddSystemMessage($"以下是用户上传的参考文档内容：\n\n{content}");
        }
        /// <summary>
        /// 处理上传的文件
        /// </summary>
        /// <param name="browserFiles"></param>
        /// <returns></returns>
        public async Task<string> ProcessUploadedFiles(params IBrowserFile[] browserFiles)
        {
            if (browserFiles == null || browserFiles.Length == 0) return string.Empty;

            var documentString = new StringBuilder();
            foreach (var file in browserFiles)
            {
                if (file.Size > 10 * 1024 * 1024 || !DocumentHelper.IsSupportedFileType(file.Name))
                {
                    continue;
                }

                var fileContent = await DocumentHelper.ExtractFromFileAsync(file);
                documentString.AppendLine($"{file.Name}: {fileContent}");
                documentString.AppendLine();
            }
            return documentString.ToString();
        }
        /// <summary>
        /// 更细腻模型配置
        /// </summary>
        /// <param name="newConfigsJson"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
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
        /// <summary>
        /// 删除会话
        /// </summary>
        /// <param name="sessionId"></param>
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