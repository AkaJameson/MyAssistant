using Microsoft.SemanticKernel;
using MyAssistant.ServiceImpl;
using System.ComponentModel;
using System.IO.Compression;
using System.Text;

namespace MyAssistant.Utils
{
    /// <summary>
    /// 项目构建器 - 供AI调用的函数集合
    /// </summary>
    public class ProjectBuilder
    {
        private readonly List<ProjectFile> _files = new();
        private string _projectName = "";
        private string _description = "";

        /// <summary>
        /// 初始化项目
        /// </summary>
        [KernelFunction]
        [Description("初始化一个新项目，设置项目名称和描述")]
        public string InitializeProject(
            [Description("项目名称")] string projectName,
            [Description("项目描述")] string description = "")
        {
            _projectName = projectName;
            _description = description;
            _files.Clear();
            return $"项目 '{projectName}' 初始化成功";
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        [KernelFunction]
        [Description("创建一个项目文件")]
        public string CreateFile(
            [Description("文件相对路径，如 src/Program.cs")] string filePath,
            [Description("文件完整内容")] string content,
            [Description("文件类型，如 csharp, java, python 等")] string fileType = "text")
        {
            try
            {
                // 验证路径
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return "错误：文件路径不能为空";
                }

                // 标准化路径
                filePath = filePath.TrimStart('/').Replace('\\', '/');

                // 检查是否已存在
                if (_files.Any(f => f.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                {
                    return $"警告：文件 {filePath} 已存在，将被覆盖";
                }

                _files.RemoveAll(f => f.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                _files.Add(new ProjectFile
                {
                    Path = filePath,
                    Content = content ?? "",
                    FileType = fileType
                });

                return $"文件 {filePath} 创建成功 ({content?.Length ?? 0} 字符)";
            }
            catch (Exception ex)
            {
                return $"创建文件 {filePath} 失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 创建目录结构（用于组织文件）
        /// </summary>
        [KernelFunction]
        [Description("创建目录结构说明文件")]
        public string CreateDirectory(
            [Description("目录路径")] string directoryPath,
            [Description("目录说明")] string description = "")
        {
            var readmePath = $"{directoryPath.TrimEnd('/')}/README.md";
            var content = $"# {directoryPath}\n\n{description}";

            return CreateFile(readmePath, content, "markdown");
        }

        /// <summary>
        /// 获取当前项目状态
        /// </summary>
        [KernelFunction]
        [Description("获取当前项目的文件列表和状态")]
        public string GetProjectStatus()
        {
            var status = new StringBuilder();
            status.AppendLine($"项目名称: {_projectName}");
            status.AppendLine($"文件总数: {_files.Count}");
            status.AppendLine("文件列表:");

            foreach (var file in _files.OrderBy(f => f.Path))
            {
                status.AppendLine($"  - {file.Path} ({file.Content.Length} 字符, {file.FileType})");
            }

            return status.ToString();
        }

        /// <summary>
        /// 完成项目构建
        /// </summary>
        [KernelFunction]
        [Description("完成项目构建，准备打包")]
        public string FinalizeProject()
        {
            if (_files.Count == 0)
            {
                return "错误：项目中没有任何文件";
            }

            return $"项目构建完成！共创建 {_files.Count} 个文件，可以进行打包";
        }

        /// <summary>
        /// 导出为ZIP
        /// </summary>
        public (string ZipBase64, bool Success, List<string> Errors) ExportToZip()
        {
            try
            {
                if (_files.Count == 0)
                {
                    return ("", false, new List<string> { "项目中没有文件" });
                }

                using var ms = new MemoryStream();
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    foreach (var file in _files)
                    {
                        var entry = archive.CreateEntry(file.Path, CompressionLevel.Optimal);
                        using var stream = entry.Open();
                        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                        writer.Write(file.Content);
                    }
                }

                var zipBase64 = Convert.ToBase64String(ms.ToArray());
                return (zipBase64, true, new List<string>());
            }
            catch (Exception ex)
            {
                return ("", false, new List<string> { ex.Message });
            }
        }

        /// <summary>
        /// 获取所有文件
        /// </summary>
        public List<ProjectFile> GetFiles() => _files.ToList();
    }

   
}