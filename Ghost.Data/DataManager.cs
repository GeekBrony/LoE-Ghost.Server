using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using Ghost.Core;

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