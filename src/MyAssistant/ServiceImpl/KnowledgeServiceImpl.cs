using LiteDB;
using MyAssistant.Core;
using MyAssistant.Data;
using MyAssistant.IServices;
using MyAssistant.Repository;

namespace MyAssistant.ServiceImpl
{
    /// <summary>
    /// 知识服务
    /// </summary>
    public class KnowledgeServiceImpl : IKnowledgeService
    {
        private readonly KnowledgeSetRepository _knowledgeSetRepository;
        private readonly KnowledgeFileRepository _knowledgeFileRepository;

        /// <summary>
        /// 知识服务
        /// </summary>
        public KnowledgeServiceImpl(KnowledgeSetRepository knowledgeSetRepository,
                                    KnowledgeFileRepository knowledgeFileRepository)
        {
            _knowledgeSetRepository = knowledgeSetRepository;
            _knowledgeFileRepository = knowledgeFileRepository;
        }
        /// <summary>
        /// 创建知识集
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public async Task<string> CreateKnowledgeSetAsync(string name, string template)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(template))
            {
                throw new ArgumentException("Name and template cannot be empty.");
            }

            string knowledgeSetId = Guid.NewGuid().ToString();
            var knowledgeSet = new KnowledgeSet
            {
                Id = new ObjectId(knowledgeSetId),
                Name = name,
                PromptTemplate = template
            };

            _knowledgeSetRepository.Insert(knowledgeSet);
            return await Task.FromResult(knowledgeSetId);
        }
        /// <summary>
        /// 查询所有知识集
        /// </summary>
        /// <returns></returns>
        public Task<List<KnowledgeSet>> QueryAllSets()
        {
            return Task.FromResult(_knowledgeSetRepository.GetAll().ToList());
        }
        public async Task<KnowledgeSet?> GetKnowledgeSetByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            return await Task.FromResult(_knowledgeSetRepository.FindById(new ObjectId(id)));
        }

        public async Task<bool> UpdateKnowledgeSetAsync(string id, string name, string template)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(template))
            {
                return false;
            }

            var knowledgeSet = _knowledgeSetRepository.FindById(new ObjectId(id));
            if (knowledgeSet == null)
            {
                return false;
            }

            knowledgeSet.Name = name;
            knowledgeSet.PromptTemplate = template;
            return await Task.FromResult(_knowledgeSetRepository.Update(knowledgeSet));
        }
        /// <summary>
        /// 删除知识集
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> DeleteKnowledgeSetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            var objectId = new ObjectId(id);
            _knowledgeFileRepository.DeleteBySetId(objectId);
            return await Task.FromResult(_knowledgeSetRepository.Delete(objectId));
        }


        public async Task<string> CreateKnowledgeFileAsync(string knowledgeSetId, string fileName, string content)
        {
            if (string.IsNullOrEmpty(knowledgeSetId) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("KnowledgeSetId, fileName, and content cannot be empty.");
            }

            // Verify the knowledge set exists
            var knowledgeSet = _knowledgeSetRepository.FindById(new ObjectId(knowledgeSetId));
            if (knowledgeSet == null)
            {
                throw new ArgumentException($"Knowledge set with ID {knowledgeSetId} does not exist.");
            }

            string knowledgeFileId = Guid.NewGuid().ToString();
            var knowledgeFile = new KnowledgeFile
            {
                Id = new ObjectId(knowledgeFileId),
                KnowledgeSetId = new ObjectId(knowledgeSetId),
                Title = fileName,
                Content = content
            };

            _knowledgeFileRepository.Insert(knowledgeFile);
            return await Task.FromResult(knowledgeFileId);
        }

        public async Task<List<KnowledgeFile>> GetKnowledgeFilesBySetIdAsync(string knowledgeSetId)
        {
            if (string.IsNullOrEmpty(knowledgeSetId))
            {
                return new List<KnowledgeFile>();
            }
            return await Task.FromResult(_knowledgeFileRepository.GetBySetId(new ObjectId(knowledgeSetId)).ToList());
        }
        public async Task<KnowledgeFile?> GetKnowledgeFileByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            return await Task.FromResult(_knowledgeFileRepository.FindById(new ObjectId(id)));
        }
        public async Task<bool> UpdateKnowledgeFileAsync(string id, string fileName, string content)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(content))
            {
                return false;
            }

            var knowledgeFile = _knowledgeFileRepository.FindById(new ObjectId(id));
            if (knowledgeFile == null)
            {
                return false;
            }

            knowledgeFile.Title = fileName;
            knowledgeFile.Content = content;
            return await Task.FromResult(_knowledgeFileRepository.Update(knowledgeFile));
        }

        public async Task<bool> DeleteKnowledgeFileAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            return await Task.FromResult(_knowledgeFileRepository.Delete(new ObjectId(id)));
        }

    }

}
