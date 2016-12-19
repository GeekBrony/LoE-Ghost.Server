using DryIoc;
using Ghost.Core.Utilities;

namespace Ghost.Core
{
    public interface ICommandManager
    {
        bool IsRunning
        {
            get; set;
        }

        IContainer Container
        {
            get;
        }

        void Execute(IUserIdentity user, string command);

        ICommandBuilder<ICommandManager> Command(string name, AccessLevel access);
    }
}