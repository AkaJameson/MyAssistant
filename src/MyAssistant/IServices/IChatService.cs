using LiteDB;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.SemanticKernel.ChatCompletion;
using MyAssistant.Data;
using MyAssistant.Models;

namespace MyAssistant.IServices
{
    // IChatService.cs
    public interface IChatService
    {
        Task<ChatSession> GetOrCreateSessionAsync(string sessionId = null);
        Task AttachFileContentToSessionAsync(string sessionId, IBrowserFile[] files);
        Task ClearSessionAsync(string sessionId);
        Task<IEnumerable<ChatSession>> GetChatSessionsAsync();
        IAsyncEnumerable<string> SendMessageStreamingAsync(string sessionId, string message);
        Task<string> ProcessUploadedFilesAsync(params IBrowserFile[] browserFiles);
        Task<ChatSession?> GetFullSessionAsync(string sessionId);
    }
}
