using LiteDB;
using MyAssistant.Core;
using MyAssistant.Repository;
using System.Text;

namespace MyAssistant.ServiceImpl
{
    public class AgentServiceImpl
    {
        private readonly KnowledgeSetRepository _setRepo;
        private readonly KnowledgeFileRepository _fileRepo;
        private ChatContext chatContext;

        public AgentServiceImpl(KnowledgeSetRepository setRepo, KnowledgeFileRepository fileRepo, ChatContext chatContext)
        {
            _setRepo = setRepo;
            _fileRepo = fileRepo;
            this.chatContext = chatContext;
        }

        public void BuildPrompt(string sessionId, ObjectId knowledgeSetId)
        {
            var set = _setRepo.FindById(knowledgeSetId);
            if (set == null)
                throw new Exception("未找到对应的知识集。");

            var files = _fileRepo.GetBySetId(knowledgeSetId).ToList();
            if (files.Count == 0)
                throw new Exception("该知识集中没有文件内容。");

            var sb = new StringBuilder();

            // 获取模板
            string template = set.PromptTemplate;

            // 将每个文件的标题占位符替换为对应的内容
            foreach (var file in files)
            {
                // 将模板中的 {{title}} 替换为对应文件的内容
                string placeholder = "{{" + file.Title + "}}";
                template = template.Replace(placeholder, file.Content);
            }
            sb.Append(template);
            string finalMessage = sb.ToString().Trim();
            chatContext.AddSystemMessage(sessionId, finalMessage);
        }
    }
}
