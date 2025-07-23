
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
// 导出为 Markdown 文件
window.exportToMarkdown = (content) => {
    try {
        const blob = new Blob([content], { type: 'text/markdown' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `conversation-${new Date().toISOString().split('T')[0]}.md`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    } catch (error) {
        console.log('exportToMarkdown error:', error);
    }
};
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

// 复制到剪贴板
window.copyToClipboard = async (text) => {
    try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            await navigator.clipboard.writeText(text);
            showToast('内容已复制到剪贴板', 'success');
        } else {
            // 回退方案
            const textArea = document.createElement('textarea');
            textArea.value = text;
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
            showToast('内容已复制到剪贴板', 'success');
        }
    } catch (err) {
        console.error('Failed to copy text: ', err);
        showToast('复制失败', 'error');
    }
};

// 初始化代码高亮（可选）
window.initializeCodeHighlight = () => {
    try {
        if (typeof Prism !== 'undefined') {
            Prism.highlightAll();
        }
    } catch (error) {
        console.log('Code highlighting not available');
    }
};

// 显示提示消息
window.showToast = (message, type = 'info') => {
    try {
        // 移除现有的 toast
        const existingToast = document.querySelector('.custom-toast');
        if (existingToast) {
            existingToast.remove();
        }

        // 创建新的 toast
        const toast = document.createElement('div');
        toast.className = `custom-toast alert alert-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'}`;
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 250px;
            opacity: 0;
            transition: opacity 0.3s ease-in-out;
        `;
        toast.textContent = message;

        document.body.appendChild(toast);

        // 显示动画
        setTimeout(() => {
            toast.style.opacity = '1';
        }, 10);

        // 3秒后自动消失
        setTimeout(() => {
            if (document.body.contains(toast)) {
                toast.style.opacity = '0';
                setTimeout(() => {
                    if (document.body.contains(toast)) {
                        document.body.removeChild(toast);
                    }
                }, 300);
            }
        }, 3000);
    } catch (error) {
        console.log('showToast error:', error);
    }
};

window.downloadBase64File = function (fileName, base64) {
    const link = document.createElement('a');
    link.href = `data:application/zip;base64,${base64}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
