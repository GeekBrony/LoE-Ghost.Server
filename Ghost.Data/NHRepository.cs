using NHibernate;
using NHibernate.Linq;
using System.Linq;

namespace Ghost.Data
{
    internal class NHRepository<T> : IRepository<T>
        where T : IEntity
    {
        private ISession m_session;
        private ISessionFactory m_factory;

        public NHRepository(ISessionFactory factory)
        {
            m_session = factory.OpenSession();
            m_factory = factory;
        }

        public T Get(int id)
        {
            return m_session.Get<T>(id);
        }

        public IQueryable<T> GetAll()
        {
            return new NhQueryable<T>(m_session.GetSessionImplementation());
        }

        public void Save(T entity)
        {
            using (var transaction = m_session.BeginTransaction())
            {
                m_session.Save(entity);
                transaction.Commit();
            }
        }

        public void Delete(T entity)
        {
            using (var transaction = m_session.BeginTransaction())
            {
                m_session.Delete(entity);
                transaction.Commit();
            }
        }

        public void Update(T entity)
        {
            using (var transaction = m_session.BeginTransaction())
            {
                m_session.Update(entity);
                transaction.Commit();
            }
        }

        public void SaveOrUpdate(T entity)
        {
            using (var transaction = m_session.BeginTransaction())
            {
                m_session.SaveOrUpdate(entity);
                transaction.Commit();
            }
        }
    }
}