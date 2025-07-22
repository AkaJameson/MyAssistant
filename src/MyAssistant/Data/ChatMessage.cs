namespace MyAssistant.Data
{
    public class ChatMessage
    {
        public int Round { get; set; }

        public string UserInput { get; set; } = "";

        public string AssistantResponse { get; set; } = "";

        public string Event { get; set; } = "";

        public List<UploadedFile> Files { get; set; } = new List<UploadedFile>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
