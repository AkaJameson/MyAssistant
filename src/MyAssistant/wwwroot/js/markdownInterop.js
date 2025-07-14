window.markdownInterop = {
    renderMarkdown: function (markdownText) {
        const md = window.markdownit();
        return md.render(markdownText);
    }
}