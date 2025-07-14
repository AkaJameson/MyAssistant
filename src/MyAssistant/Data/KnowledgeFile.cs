using LiteDB;

namespace MyAssistant.Data
{
    public class KnowledgeFile
    {
        public ObjectId Id { get; set; }
        public string Title { get; set; } = "";    
        public string Content { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public ObjectId KnowledgeSetId { get; set; }
    }

}
