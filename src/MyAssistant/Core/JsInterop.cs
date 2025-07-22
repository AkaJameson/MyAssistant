using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace MyAssistant.Components
{
    public class JsInterop
    {
        private readonly IJSRuntime _js;

        public JsInterop(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>
        /// 渲染 Markdown 字符串为 HTML
        /// </summary>
        public async Task<string> RenderMarkdown(string markdown)
        {
            return await _js.InvokeAsync<string>("markdownInterop.renderMarkdown", markdown);
        }

        /// <summary>
        /// 阻止 Enter 键默认行为（无 shift）
        /// </summary>
        public async Task PreventDefaultEnter(string id)
        {
            await _js.InvokeVoidAsync("preventDefaultEnter", id);
        }

        /// <summary>
        /// 导出 Markdown 文件
        /// </summary>
        public async Task ExportToMarkdown(string content)
        {
            await _js.InvokeVoidAsync("exportToMarkdown", content);
        }

        /// <summary>
        /// 滚动到底部
        /// </summary>
        public async Task ScrollToBottom(string elementId)
        {
            await _js.InvokeVoidAsync("scrollToBottom", elementId);
        }
    }
}
