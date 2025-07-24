using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace MyAssistant.Utils;
public static class ProjectParser
{
    // 支持多种文件头格式
    private static readonly Regex[] FileHeaderPatterns = new[]
    {
        new Regex(@"^##\s+(/[^\r\n]*)", RegexOptions.Compiled | RegexOptions.Multiline), // ## /path/file
        new Regex(@"^##\s+([^\r\n/][^\r\n]*)", RegexOptions.Compiled | RegexOptions.Multiline), // ## path/file
        new Regex(@"^\d+\.\s+([^\r\n]*)", RegexOptions.Compiled | RegexOptions.Multiline), // 1. path/file
    };

    private static readonly Regex CodeBlockPattern = new Regex(@"```(\w+)?\r?\n(.*?)```", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// 解析AI回复的markdown格式，提取文件路径和内容
    /// </summary>
    public static List<(string Path, string Content)> Parse(string markdown)
    {
        var files = new List<(string Path, string Content)>();

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return files;
        }

        // 标准化换行符
        markdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

        // 尝试不同的解析模式
        files = TryParseWithPatterns(markdown);

        // 如果没有解析到文件，尝试基于数字编号的格式
        if (files.Count == 0)
        {
            files = ParseNumberedFormat(markdown);
        }

        return files;
    }

    private static List<(string Path, string Content)> TryParseWithPatterns(string markdown)
    {
        foreach (var pattern in FileHeaderPatterns)
        {
            var files = ParseWithPattern(markdown, pattern);
            if (files.Count > 0)
            {
                return files;
            }
        }
        return new List<(string Path, string Content)>();
    }

    private static List<(string Path, string Content)> ParseWithPattern(string markdown, Regex pattern)
    {
        var files = new List<(string Path, string Content)>();
        var fileMatches = pattern.Matches(markdown);

        for (int i = 0; i < fileMatches.Count; i++)
        {
            var currentMatch = fileMatches[i];
            var filePath = currentMatch.Groups[1].Value.Trim();

            // 确定文件内容的起始和结束位置
            int contentStart = currentMatch.Index + currentMatch.Length;
            int contentEnd = (i < fileMatches.Count - 1)
                ? fileMatches[i + 1].Index
                : markdown.Length;

            var fileSection = markdown.Substring(contentStart, contentEnd - contentStart);
            var content = ExtractFileContent(fileSection);

            if (!string.IsNullOrEmpty(content))
            {
                files.Add((NormalizePath(filePath), content.Trim()));
            }
        }

        return files;
    }

    /// <summary>
    /// 解析基于数字编号的格式，如 "1. src/Program.cs"
    /// </summary>
    private static List<(string Path, string Content)> ParseNumberedFormat(string markdown)
    {
        var files = new List<(string Path, string Content)>();
        var lines = markdown.Split('\n');
        string currentPath = null;
        var contentBuilder = new StringBuilder();
        bool inCodeBlock = false;
        bool collectingContent = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // 检查是否是文件头（数字编号格式）
            var numberMatch = Regex.Match(line, @"^\d+\.\s+(.+)$");
            if (numberMatch.Success)
            {
                // 保存前一个文件
                if (currentPath != null && contentBuilder.Length > 0)
                {
                    files.Add((NormalizePath(currentPath), contentBuilder.ToString().Trim()));
                }

                currentPath = numberMatch.Groups[1].Value;
                contentBuilder.Clear();
                collectingContent = false;
                inCodeBlock = false;
                continue;
            }

            // 如果有当前路径，开始收集内容
            if (currentPath != null)
            {
                // 检查代码块标记
                if (line.StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        collectingContent = true;
                        continue; // 跳过开始标记
                    }
                    else
                    {
                        inCodeBlock = false;
                        continue; // 跳过结束标记
                    }
                }

                // 收集内容
                if (collectingContent)
                {
                    contentBuilder.AppendLine(lines[i]); // 使用原始行保持缩进
                }
                else if (!inCodeBlock && !string.IsNullOrWhiteSpace(line))
                {
                    // 如果不在代码块中但有内容，也开始收集
                    collectingContent = true;
                    contentBuilder.AppendLine(lines[i]);
                }
            }
        }

        // 保存最后一个文件
        if (currentPath != null && contentBuilder.Length > 0)
        {
            files.Add((NormalizePath(currentPath), contentBuilder.ToString().Trim()));
        }

        return files;
    }

    /// <summary>
    /// 从文件段落中提取实际的文件内容
    /// </summary>
    private static string ExtractFileContent(string fileSection)
    {
        if (string.IsNullOrWhiteSpace(fileSection))
        {
            return string.Empty;
        }

        // 查找代码块
        var codeBlockMatch = CodeBlockPattern.Match(fileSection);
        if (codeBlockMatch.Success)
        {
            return codeBlockMatch.Groups[2].Value;
        }

        // 如果没有代码块，提取普通文本内容
        var lines = fileSection.Split('\n');
        var contentLines = new List<string>();
        bool startCapturing = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 跳过空行直到找到实际内容
            if (!startCapturing && string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            // 跳过markdown代码块标记
            if (trimmedLine.StartsWith("```"))
            {
                startCapturing = !startCapturing;
                continue;
            }

            if (startCapturing || !string.IsNullOrEmpty(trimmedLine))
            {
                startCapturing = true;
                contentLines.Add(line);
            }
        }

        return string.Join("\n", contentLines);
    }

    /// <summary>
    /// 标准化文件路径
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "unknown_file.txt";
        }

        // 移除开头的斜杠
        path = path.TrimStart('/');

        // 替换反斜杠为正斜杠
        path = path.Replace('\\', '/');

        // 确保路径不为空
        if (string.IsNullOrEmpty(path))
        {
            return "unknown_file.txt";
        }

        return path;
    }

    /// <summary>
    /// 创建zip文件的base64编码
    /// </summary>
    public static string CreateZipBase64(List<(string Path, string Content)> files)
    {
        if (files == null || files.Count == 0)
        {
            throw new ArgumentException("文件列表不能为空", nameof(files));
        }

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var (path, content) in files)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                try
                {
                    var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
                    using var stream = entry.Open();
                    using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                    writer.Write(content ?? string.Empty);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"创建文件 {path} 时出错: {ex.Message}");
                }
            }
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// 调试方法：打印解析结果
    /// </summary>
    public static void DebugPrint(string markdown)
    {
        Console.WriteLine("=== 原始Markdown ===");
        Console.WriteLine(markdown);
        Console.WriteLine("\n=== 解析结果 ===");

        var files = Parse(markdown);
        foreach (var (path, content) in files)
        {
            Console.WriteLine($"文件: {path}");
            Console.WriteLine($"内容长度: {content.Length}");
            Console.WriteLine($"内容预览: {content.Substring(0, Math.Min(100, content.Length))}...");
            Console.WriteLine("---");
        }
    }

    public static (string ZipBase64, bool Success, List<string> Errors) ParseAndCreateProject(string markdown)
    {
        try
        {
            var files = Parse(markdown);

            if (files.Count == 0)
            {
                return (string.Empty, false, new List<string> { "没有解析到任何文件，可能是格式不支持" });
            }

            var zipBase64 = CreateZipBase64(files);
            return (zipBase64, true, new List<string>());
        }
        catch (Exception ex)
        {
            return (string.Empty, false, new List<string> { $"处理过程中出错: {ex.Message}" });
        }
    }
}