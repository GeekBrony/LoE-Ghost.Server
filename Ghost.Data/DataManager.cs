using Ghost.Core;
using NHibernate;
using System;

namespace Ghost.Data
{
    internal class DataManager : IDataManager
    {

        public DataManager(ISettingsManager settings)
        {

        }

        public ITransaction BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public ISession GetSession()
        {
            throw new NotImplementedException();
        }
    }
}