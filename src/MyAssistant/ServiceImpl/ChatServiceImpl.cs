using Microsoft.AspNetCore.Components.Forms;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MyAssistant.Core;
using MyAssistant.Data;
using MyAssistant.IServices;
using MyAssistant.Repository;
using MyAssistant.Utils;
using System.Text;

namespace MyAssistant.ServiceImpl
{
    public class ChatServiceImpl : IChatService
    {
        private readonly ChatSessionRepository _sessionRepo;
        private readonly ChatContext _chatContext;
        private readonly KernelContext _kernelContext;
        private readonly ILogger<ChatServiceImpl> _logger;

        public ChatServiceImpl(
            ChatSessionRepository sessionRepo,
            ChatContext chatContext,
            KernelContext kernelContext,
            ILogger<ChatServiceImpl> logger)
        {
            _sessionRepo = sessionRepo;
            _chatContext = chatContext;
            _kernelContext = kernelContext;
            _logger = logger;
        }

        public async Task<ChatSession> GetOrCreateSessionAsync(string sessionId = null)
        {
            sessionId ??= Guid.NewGuid().ToString();

            // 从数据库加载会话（如果存在）
            var dbSession = _sessionRepo.FindBySessionId(sessionId);
            if (dbSession != null)
            {
                // 将会话历史加载到ChatContext
                var history = _chatContext.GetOrCreateChatHistory(sessionId);
                foreach (var msg in dbSession.Messages)
                {
                    if (!string.IsNullOrWhiteSpace(msg.UserInput))
                        history.AddUserMessage(msg.UserInput);
                    if (!string.IsNullOrWhiteSpace(msg.AssistantResponse))
                        history.AddAssistantMessage(msg.AssistantResponse);
                }
                return dbSession;
            }

            // 创建新会话
            var newSession = new ChatSession
            {
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
            _sessionRepo.Insert(newSession);

            // 初始化内存中的历史
            _chatContext.GetOrCreateChatHistory(sessionId);
            return newSession;
        }

        public async IAsyncEnumerable<string> SendMessageStreamingAsync(string sessionId, string message)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty.");

            var history = _chatContext.GetOrCreateChatHistory(sessionId);
            history.AddUserMessage(message);

            // 保存用户消息到数据库
            var dbSession = _sessionRepo.FindBySessionId(sessionId);
            if (dbSession == null)
            {
                dbSession = new ChatSession { SessionId = sessionId };
                _sessionRepo.Insert(dbSession);
            }
            var chatMessage = new ChatMessage
            {
                UserInput = message,
                Timestamp = DateTime.UtcNow,
                Round = dbSession.Messages.Count + 1
            };
            dbSession.Messages.Add(chatMessage);
            _sessionRepo.Update(dbSession);

            // 获取聊天服务
            var chatCompletion = _kernelContext.Current.GetRequiredService<IChatCompletionService>();
            var settings = new OpenAIPromptExecutionSettings { MaxTokens = 100000 };

            // 流式响应
            var fullResponse = new StringBuilder();
            await foreach (var content in chatCompletion.GetStreamingChatMessageContentsAsync(history, settings))
            {
                if (content.Content != null)
                {
                    fullResponse.Append(content.Content);
                    yield return content.Content;
                }
            }

            // 添加助手消息到历史
            history.AddAssistantMessage(fullResponse.ToString());

            // 更新数据库中的助手回复
            chatMessage.AssistantResponse = fullResponse.ToString();
            chatMessage.Timestamp = DateTime.UtcNow;
            _sessionRepo.Update(dbSession);
        }

        public async Task AttachFileContentToSessionAsync(string sessionId, IBrowserFile[] files)
        {
            if (files == null || files.Length == 0) return;

            var content = await ProcessUploadedFilesAsync(files);
            if (string.IsNullOrWhiteSpace(content)) return;

            var history = _chatContext.GetOrCreateChatHistory(sessionId);
            history.AddSystemMessage($"以下是用户上传的参考文档内容：\n\n{content}");

            // 保存到数据库
            var dbSession = _sessionRepo.FindBySessionId(sessionId);
            if (dbSession != null)
            {
                dbSession.Messages.Add(new ChatMessage
                {
                    Event = "FileUpload",
                    UserInput = $"上传了 {files.Length} 个文件",
                    Timestamp = DateTime.UtcNow
                });
                _sessionRepo.Update(dbSession);
            }
        }
        /// <summary>
        /// 处理上传的文件
        /// </summary>
        /// <param name="browserFiles"></param>
        /// <returns></returns>
        public async Task<string> ProcessUploadedFilesAsync(params IBrowserFile[] browserFiles)
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
        public async Task ClearSessionAsync(string sessionId)
        {
            _chatContext.RemoveChatHistory(sessionId);
            var chatSession = _sessionRepo.FindBySessionId(sessionId);
            if (chatSession != null)
            {
                _sessionRepo.Delete(chatSession.Id);
            }
        }

        public async Task<IEnumerable<ChatSession>> GetChatSessionsAsync()
        {
            return _sessionRepo.GetAllSummery().ToList();
        }
    }
}
