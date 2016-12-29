using DryIoc;
using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Data.Entities;
using System.Collections.Concurrent;
using System.Linq;

namespace Ghost.Data
{
    internal class UsersManager : IUsersManager
    {
        private ConcurrentDictionary<int, IUserIdentity> m_users;

        public IContainer Container
        {
            get;
            set;
        }

        public UsersManager()
        {
            m_users = new ConcurrentDictionary<int, IUserIdentity>();
        }

        public bool TryGetById(int id, out IUserIdentity user)
        {
            if (!m_users.TryGetValue(id, out user))
            {
                var userAccount = Container.Resolve<IRepository<UserAccount>>().Get(id);
                if (userAccount != null)
                {
                    user = CreateIdentity(userAccount);
                    return true;
                }
            }
            return false;
        }

        public bool TryGetByName(string name, out IUserIdentity user)
        {
            user = m_users.FirstOrDefault(x => x.Value.Name == name).Value;
            if (user == null)
            {
                var userAccount = Container.Resolve<IRepository<UserAccount>>().GetAll().Where(x => x.Name == name).FirstOrDefault();
                if (userAccount != null)
                {
                    user = CreateIdentity(userAccount);
                    return true;
                }
            }
            return false;
        }

        private IUserIdentity CreateIdentity(UserAccount account)
        {
            var identity = new UserIdentity(account);
            m_users[account.Id] = identity;
            return identity;
        }
    }

    internal class UserIdentity : IUserIdentity
    {
        private UserAccount m_account;

        public string Name => m_account.Name;

        public AccessLevel Access => m_account.Access;

        public UserIdentity(UserAccount account)
        {
            m_account = account;
        }

        //ToDo: per user permission
        public bool CheckPermission(UserPermission permission)
        {
            return true;
        }

        public void Error(string message)
        {
            //throw new NotImplementedException();
        }

        public void Info(string message)
        {
            //throw new NotImplementedException();
        }
    }
}