using LiteDB;
using MyAssistant.Data;
using MyAssistant.IServices;
using System.Text;

namespace MyAssistant.ServiceImpl
{
    public class AgentServiceImpl
    {
        private readonly KnowledgeSetRepository _setRepo;
        private readonly KnowledgeFileRepository _fileRepo;
        private IChatContext chatContext;

        public AgentServiceImpl(KnowledgeSetRepository setRepo, KnowledgeFileRepository fileRepo, IChatContext chatContext)
        {
            _setRepo = setRepo;
            _fileRepo = fileRepo;
            this.chatContext = chatContext;
        }
        public void AddKnowledgeToSystemMessage(string sessionId, ObjectId knowledgeSetId)
        {
            var set = _setRepo.FindById(knowledgeSetId);
            if (set == null)
                throw new Exception("未找到对应的知识集。");

            var files = _fileRepo.GetBySetId(knowledgeSetId).ToList();
            if (files.Count == 0)
                throw new Exception("该知识集中没有文件内容。");

            var sb = new StringBuilder();

            foreach (var file in files)
            {
                string message = set.PromptTemplate;

                // 简单替换模板中 {{标题}} 和 {{内容}}
                message = message.Replace("{{标题}}", file.Title)
                                 .Replace("{{内容}}", file.Content);

                sb.AppendLine(message);
                sb.AppendLine(); // 换行分隔
            }

            string finalMessage = sb.ToString().Trim();
            chatContext.AddSystemMessage(sessionId, finalMessage);
        }
     
    }
}
