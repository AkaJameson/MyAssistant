using Microsoft.AspNetCore.Components.Forms;
using Microsoft.SemanticKernel;
using MyAssistant.Data;

namespace MyAssistant.IServices
{
    /// <summary>
    /// 统一会话管理
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// 删除一个会话
        /// </summary>
        /// <param name="sessionId"></param>
        void ClearSession(string sessionId);
        /// <summary>
        /// 获取model配置
        /// </summary>
        /// <returns></returns>
        string GetModelConfigs();
        /// <summary>
        /// 获取会话
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="input"></param>
        /// <param name="contextLength"></param>
        /// <returns></returns>
        Task StartStreamingChatSession(string sessionId, string input, int contextLength = 20, params IBrowserFile[] browserFile);
        /// <summary>
        /// 更新内核
        /// </summary>
        /// <param name="modelName"></param>
        /// <returns></returns>
        Task UpdateKernel(string modelName);
        /// <summary>
        /// 更新model配置
        /// </summary>
        /// <param name="newConfigsJson"></param>
        /// <returns></returns>
        Task UpdateModelConfigs(string newConfigsJson);
        /// <summary>
        /// 获取所有会话摘要
        /// </summary>
        /// <returns></returns>
        List<ChatSession> GetChatSessions();
    }
}
