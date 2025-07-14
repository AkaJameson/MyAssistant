using LiteDB;

namespace MyAssistant.Data
{
    public class KnowledgeSet
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";           
        public string PromptTemplate { get; set; } = "";    
    }

}
