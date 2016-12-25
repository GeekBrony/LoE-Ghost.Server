using Ghost.Core.Utilities;

namespace Ghost.Core
{
    public interface ICommandManager
    {
        IGhostApplication Application
        {
            get;
        }

        void Execute(IUserIdentity user, string command);

        ICommandBuilder<ICommandManager> Command(string name, AccessLevel access);
    }
}