using Microsoft.SemanticKernel;

namespace MyAssistant.IServices
{
    /// <summary>
    /// 统一会话管理
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// 非流式
        /// </summary>
        /// <param name="kernel"></param>
        /// <returns></returns>
        Task StartChatSession(Kernel kernel,string input);
        /// <summary>
        /// 流式
        /// </summary>
        /// <param name="kernel"></param>
        /// <returns></returns>
        Task StartStreamingChatSession(Kernel kernel,string input);
    }
}
