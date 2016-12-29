using System.Linq;

namespace Ghost.Data
{
    public interface IRepository<T>
        where T : IEntity
    {
        T Get(int id);

        IQueryable<T> GetAll();

        void Save(T entity);

        void Delete(T entity);

        void Update(T entity);

        void SaveOrUpdate(T entity);
    }
}