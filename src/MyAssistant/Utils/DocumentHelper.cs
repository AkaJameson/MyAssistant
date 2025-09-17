using Aspose.Words;
using Microsoft.AspNetCore.Components.Forms;
using System.Text;

namespace MyAssistant.Utils
{
    public static class DocumentHelper
    {
        private const long MaxFileSize = 10 * 1024 * 1024;
        private static readonly List<string> supportedTypes = new List<string>()
        {
            ".pdf",
            ".doc",
            ".docx",
            ".md",
            ".txt"
        };

        /// <summary>
        /// 从上传的文件中提取文本内容
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <returns>提取的文本内容</returns>
        public static async Task<string> ExtractFromFileAsync(IBrowserFile file)
        {
            if (file == null)
                return string.Empty;

            // 获取文件扩展名
            var extension = Path.GetExtension(file.Name).ToLower();

            // 检查是否支持的文件类型
            if (!supportedTypes.Contains(extension))
                return string.Empty;

            try
            {
                // 根据文件类型处理
                switch (extension)
                {
                    case ".txt":
                    case ".md":
                        return await ExtractTextFromPlainFileAsync(file);

                    case ".pdf":
                    case ".doc":
                    case ".docx":
                        return await ExtractTextFromDocumentAsync(file);

                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("文件处理失败", ex);
            }
        }

        /// <summary>
        /// 提取纯文本文件内容
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <returns>文本内容</returns>
        private static async Task<string> ExtractTextFromPlainFileAsync(IBrowserFile file)
        {
            using var stream = file.OpenReadStream(MaxFileSize);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// 使用 Aspose.Words 提取文档内容
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <returns>文档文本内容</returns>
        private static async Task<string> ExtractTextFromDocumentAsync(IBrowserFile file)
        {
            using var stream = file.OpenReadStream(MaxFileSize);

            // 将流转换为字节数组
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            // 使用 Aspose.Words 加载文档
            using var docStream = new MemoryStream(bytes);
            var doc = new Document(docStream);

            // 提取纯文本
            return doc.ToString(SaveFormat.Text);
        }

        /// <summary>
        /// 检查文件类型是否受支持
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否支持</returns>
        public static bool IsSupportedFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return supportedTypes.Contains(extension);
        }

        /// <summary>
        /// 获取支持的文件类型列表
        /// </summary>
        /// <returns>支持的文件类型</returns>
        public static List<string> GetSupportedTypes()
        {
            return new List<string>(supportedTypes);
        }
    }
}