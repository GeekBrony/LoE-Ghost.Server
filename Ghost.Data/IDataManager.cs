using NHibernate;

namespace Ghost.Data
{
    public interface IDataManager
    {
        ISession GetSession();

        ITransaction BeginTransaction();
    }
}