using LiteDB;
using MyAssistant.Data;

namespace MyAssistant.Repository
{
    public class KnowledgeFileRepository
    {
        private readonly ILiteCollection<KnowledgeFile> _collection;

        public KnowledgeFileRepository(LiteDatabase db)
        {
            _collection = db.GetCollection<KnowledgeFile>("knowledge_files");
        }

        public ObjectId Insert(KnowledgeFile file) => _collection.Insert(file);
        public bool Update(KnowledgeFile file) => _collection.Update(file);
        public bool Delete(ObjectId id) => _collection.Delete(id);
        public KnowledgeFile? FindById(ObjectId id) => _collection.FindById(id);
        public IEnumerable<KnowledgeFile> GetBySetId(ObjectId setId) =>
            _collection.Find(x => x.KnowledgeSetId == setId);
        public IEnumerable<KnowledgeFile> GetAll() => _collection.FindAll();
        public bool DeleteBySetId(ObjectId setId)
        {
            var files = _collection.Find(x => x.KnowledgeSetId == setId);
            if (files.Count() == 0)
            {
                return false;
            }
            foreach (var file in files)
            {
                _collection.Delete(file.Id);
            }
            return true;
        }
    }
}
