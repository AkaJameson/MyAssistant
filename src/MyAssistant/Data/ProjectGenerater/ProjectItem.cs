using System.Text.Json.Serialization;

namespace MyAssistant.Data.ProjectGenerater
{
    public class ProjectItem
    {
        [JsonPropertyName("type")]
        public ItemType Type { get; set; } = ItemType.File;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("isEntryPoint")]
        public bool IsEntryPoint { get; set; }
    }

    public enum ItemType
    {
        File,
        Directory
    }
}
