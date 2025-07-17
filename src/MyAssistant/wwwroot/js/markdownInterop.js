window.markdownInterop = {
    renderMarkdown: function (markdownText) {
        const md = window.markdownit();
        return md.render(markdownText);
    }
}

window.scrollToBottom = function (element) {
    element.scrollTop = element.scrollHeight;
};