using Ghost.Core.Utilities;

namespace Ghost.Core
{
    public interface IUserIdentity
    {
        AccessLevel Access
        {
            get;
        }

        void Info(string message);

        void Error(string message);

        bool CheckPermission(UserPermission permission);
    }
}