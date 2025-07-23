namespace MyAssistant.Data.ProjectGenerater
{
    public class ProjectStructure
    {
        public string ProjectName { get; set; } = string.Empty;
        public List<ProjectItem> Items { get; set; } = new();
    }
}
