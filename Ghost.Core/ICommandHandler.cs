using Ghost.Core.Utilities;

namespace Ghost.Core
{
    public interface ICommandHandler
    {
        string Name
        {
            get;
        }

        bool IsAlias
        {
            get;
        }

        string FullName
        {
            get;
        }

        AccessLevel Access
        {
            get;
        }

        bool IsSubcommand
        {
            get;
        }

        bool CheckPermission(IUserIdentity user);

        bool IsSubcommandOf(ICommandHandler handler);

        void Execute(IUserIdentity user, CommandArgs args);
    }
}