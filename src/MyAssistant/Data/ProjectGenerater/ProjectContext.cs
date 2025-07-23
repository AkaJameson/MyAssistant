namespace MyAssistant.Data.ProjectGenerater
{
    public class ProjectContext
    {
        public ProjectStructure Structure { get; set; }
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }
}
