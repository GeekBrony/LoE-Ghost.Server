using NHibernate.Linq;
using System.Linq;

namespace Ghost.Data
{
    internal class NHRepository<T> : IRepository<T>
        where T : IEntity
    {
        private IDataManager m_manager;

        public NHRepository(IDataManager manager)
        {
            m_manager = manager;
        }

        public T Get(int id)
        {
            return m_manager.GetSession().Get<T>(id);
        }

        public IQueryable<T> GetAll()
        {
            return new NhQueryable<T>(m_manager.GetSession().GetSessionImplementation());
        }

        public void Save(T entity)
        {
            using (var transaction = m_manager.BeginTransaction())
            {
                m_manager.GetSession().Save(entity);
                transaction.Commit();
            }
        }

        public void Delete(T entity)
        {
            using (var transaction = m_manager.BeginTransaction())
            {
                m_manager.GetSession().Delete(entity);
                transaction.Commit();
            }
        }

        public void Update(T entity)
        {
            using (var transaction = m_manager.BeginTransaction())
            {
                m_manager.GetSession().Update(entity);
                transaction.Commit();
            }
        }

        public void SaveOrUpdate(T entity)
        {
            using (var transaction = m_manager.BeginTransaction())
            {
                m_manager.GetSession().SaveOrUpdate(entity);
                transaction.Commit();
            }
        }
    }
}