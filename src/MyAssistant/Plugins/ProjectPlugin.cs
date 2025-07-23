using Microsoft.SemanticKernel;
using MyAssistant.Data.ProjectGenerater;
using MyAssistant.Plugins;
using System.ComponentModel;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

public class ProjectPlugin
{
    private readonly ProjectPromptPlugin _promptPlugin;

    public ProjectPlugin(
        ProjectPromptPlugin promptPlugin)
    {
        _promptPlugin = promptPlugin;
    }

    [KernelFunction("CreateProject")]
    public async Task<string> CreateProject(
        Kernel kernel,
        [Description("项目描述")] string description,
        [Description("项目名称")] string projectName,
        [Description("技术栈")] string techStack = "")
    {
        try
        {
            // 1. 生成项目结构JSON
            var json = await _promptPlugin.GenerateProjectStructure(
                kernel, projectName, description, techStack);

            // 2. 解析JSON
            var project = JsonSerializer.Deserialize<ProjectStructure>(json);

            // 3. 创建ZIP文件
            return await CreateProjectZip(project);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task<string> CreateProjectZip(ProjectStructure project)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var item in project.Items)
            {
                if (item.Type == ItemType.Directory) continue;

                var entry = archive.CreateEntry(item.Path);
                using var entryStream = entry.Open();
                var contentBytes = Encoding.UTF8.GetBytes(item.Content);
                await entryStream.WriteAsync(contentBytes);
            }
        }
        return Convert.ToBase64String(memoryStream.ToArray());
    }
}
