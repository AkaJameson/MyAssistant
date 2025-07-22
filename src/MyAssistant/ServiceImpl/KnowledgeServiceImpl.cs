using MyAssistant.Data;
using MyAssistant.IServices;
using MyAssistant.Repository;
using LiteDB;
using System.Linq.Expressions;

namespace MyAssistant.ServiceImpl
{
    public class KnowledgeServiceImpl : IKnowledgeService
    {
        private readonly KnowledgeSetRepository _setRepo;
        private readonly KnowledgeFileRepository _fileRepo;
        private readonly ILogger<KnowledgeServiceImpl> _logger;

        public KnowledgeServiceImpl(
            KnowledgeSetRepository setRepo,
            KnowledgeFileRepository fileRepo,
            ILogger<KnowledgeServiceImpl> logger)
        {
            _setRepo = setRepo;
            _fileRepo = fileRepo;
            _logger = logger;
        }

        public async Task<string> CreateKnowledgeSetAsync(string name, string template)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("知识集名称不能为空");
            if (string.IsNullOrWhiteSpace(template))
                throw new ArgumentException("提示模板不能为空");

            var knowledgeSet = new KnowledgeSet
            {
                Name = name,
                PromptTemplate = template
            };

            var id = _setRepo.Insert(knowledgeSet);
            _logger.LogInformation($"创建知识集: {name} (ID: {id})");
            return id.ToString();
        }

        public async Task<string> CreateKnowledgeFileAsync(string knowledgeSetId, string fileName, string content)
        {
            var setId = new ObjectId(knowledgeSetId);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("文件名不能为空");
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("文件内容不能为空");

            // 验证知识集存在
            var set = _setRepo.FindById(new ObjectId(knowledgeSetId));
            if (set == null)
                throw new KeyNotFoundException($"找不到ID为{knowledgeSetId}的知识集");

            var knowledgeFile = new KnowledgeFile
            {
                KnowledgeSetId = setId,
                Title = fileName,
                Content = content
            };

            var id = _fileRepo.Insert(knowledgeFile);
            _logger.LogInformation($"在知识集 {set.Name} 中创建文件: {fileName} (ID: {id})");
            return id.ToString();
        }

        public async Task<bool> DeleteKnowledgeSetAsync(string id)
        {
            var objectId = new ObjectId(id);

            // 先删除关联的文件
            _fileRepo.DeleteBySetId(objectId);

            // 再删除知识集
            var result = _setRepo.Delete(objectId);
            if (result)
            {
                _logger.LogInformation($"删除知识集 (ID: {id})");
            }
            return result;
        }

        public async Task<bool> DeleteKnowledgeFileAsync(string id)
        {
            var objectId = new ObjectId(id);

            var file = _fileRepo.FindById(objectId);
            var result = _fileRepo.Delete(objectId);

            if (result && file != null)
            {
                _logger.LogInformation($"删除知识文件: {file.Title} (ID: {id})");
            }
            return result;
        }

        public async Task<KnowledgeFile?> GetKnowledgeFileByIdAsync(string id)
        {
            var objectId = new ObjectId(id);

            return _fileRepo.FindById(objectId);
        }

        public async Task<List<KnowledgeFile>> GetKnowledgeFilesBySetIdAsync(string knowledgeSetId)
        {
            var objectId = new ObjectId(knowledgeSetId);

            return _fileRepo.GetBySetId(objectId).ToList();
        }

        public async Task<KnowledgeSet?> GetKnowledgeSetByIdAsync(string id)
        {
            var objectId = new ObjectId(id);


            return _setRepo.FindById(objectId);
        }

        public async Task<List<KnowledgeSet>> QueryAllSetsAsync()
        {
            return _setRepo.GetAll().ToList();
        }

        public async Task<bool> UpdateKnowledgeSetAsync(string id, string name, string template)
        {
            var objectId = new ObjectId(id);

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("知识集名称不能为空");
            if (string.IsNullOrWhiteSpace(template))
                throw new ArgumentException("提示模板不能为空");

            var knowledgeSet = _setRepo.FindById(objectId);
            if (knowledgeSet == null)
                return false;

            knowledgeSet.Name = name;
            knowledgeSet.PromptTemplate = template;

            var result = _setRepo.Update(knowledgeSet);
            if (result)
            {
                _logger.LogInformation($"更新知识集: {name} (ID: {id})");
            }
            return result;
        }

        public async Task<bool> UpdateKnowledgeFileAsync(string id, string fileName, string content)
        {
            var objectId = new ObjectId(id);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("文件名不能为空");
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("文件内容不能为空");

            var knowledgeFile = _fileRepo.FindById(objectId);
            if (knowledgeFile == null)
                return false;

            knowledgeFile.Title = fileName;
            knowledgeFile.Content = content;

            var result = _fileRepo.Update(knowledgeFile);
            if (result)
            {
                _logger.LogInformation($"更新知识文件: {fileName} (ID: {id})");
            }
            return result;
        }

        public async Task<List<KnowledgeSet>> SearchSetsAsync(string keyword)
        {
            Expression<Func<KnowledgeSet, bool>> predicate = set =>
                set.Name.Contains(keyword) ||
                set.PromptTemplate.Contains(keyword);

            return _setRepo.Find(predicate).ToList();
        }
    }
}
