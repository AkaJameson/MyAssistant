using LiteDB;
using MyAssistant.Data;
using System.Linq.Expressions;

namespace MyAssistant.Repository
{
    public class KnowledgeSetRepository
    {
        private readonly ILiteCollection<KnowledgeSet> _collection;

        public KnowledgeSetRepository(LiteDatabase db)
        {
            _collection = db.GetCollection<KnowledgeSet>("knowledge_sets");
        }

        public ObjectId Insert(KnowledgeSet set) => _collection.Insert(set);
        public bool Update(KnowledgeSet set) => _collection.Update(set);
        public bool Delete(ObjectId id) => _collection.Delete(id);
        public KnowledgeSet? FindById(ObjectId id) => _collection.FindById(id);
        public IEnumerable<KnowledgeSet> GetAll() => _collection.FindAll();
        public IEnumerable<KnowledgeSet> Find(Expression<Func<KnowledgeSet, bool>> predicate) => _collection.Find(predicate);

    }
}
