namespace MyAssistant.Data
{
    public class ChatMessage
    {
        public int Round { get; set; }

        public string UserInput { get; set; } = "";

        public string AssistantResponse { get; set; } = "";

        public string Event { get; set; } = "";

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
