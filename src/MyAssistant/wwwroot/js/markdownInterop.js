
window.renderMarkdown = function (markdownText) {
    // 确保 markdown-it 已加载
    if (!window.markdownit) {
        console.error("markdownit is not loaded");
        return markdownText;
    }

    const md = window.markdownit({
        html: true,
        linkify: true,
        typographer: true,
        highlight: function (str, lang) {
            // 使用 highlight.js 进行代码高亮
            if (lang && hljs.getLanguage(lang)) {
                try {
                    return '<pre class="hljs"><code>' +
                        hljs.highlight(str, { language: lang, ignoreIllegals: true }).value +
                        '</code></pre>';
                } catch (__) { }
            }

            return '<pre class="hljs"><code>' + md.utils.escapeHtml(str) + '</code></pre>';
        }
    });

    return md.render(markdownText);
};

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
function preventDefaultEnter(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault();
            }
        });
    }
}

// 滚动到顶部
window.scrollToTop = () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

window.setRenderingState = function (element, isRendering) {
    if (isRendering) {
        element.classList.add('rendering');
    } else {
        element.classList.remove('rendering');
    }
};