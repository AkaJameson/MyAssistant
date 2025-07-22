
window.markdownInterop = {
    renderMarkdown: function (markdownText) {
        const md = window.markdownit();
        return md.render(markdownText);
    }
}

window.attachEnterKeyHandler = function (elementId) {
    const input = document.getElementById(elementId);
    if (!input) return;

    input.addEventListener('keydown', function (event) {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault(); // 阻止换行
        }
    });
};


// 导出Markdown文件
function exportToMarkdown(content) {
    const blob = new Blob([content], { type: 'text/markdown' });
    const url = URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = `ai-assistant-${new Date().toISOString().slice(0, 10)}.md`;
    document.body.appendChild(a);
    a.click();

    setTimeout(() => {
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
    }, 0);
}

// 滚动到消息底部
function scrollToBottom(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}


// 滚动到顶部
window.scrollToTop = () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};
