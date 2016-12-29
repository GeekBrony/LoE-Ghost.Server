using Ghost.Core.Utilities;

namespace Ghost.Core
{
    public interface IUserIdentity
    {
        string Name
        {
            get;
        }

        AccessLevel Access
        {
            get;
        }

        void Info(string message);

        void Error(string message);

        bool CheckPermission(UserPermission permission);
    }
}