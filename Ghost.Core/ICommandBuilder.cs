using Ghost.Core.Utilities;

namespace Ghost.Core
{
    public interface ICommandBuilder
    {
        string Name
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

        ICommandManager Manager
        {
            get;
        }
    }

    public interface ICommandBuilder<T> : ICommandBuilder
    {
        T Previous();

        ICommandBuilder<T> Alias(string name);

        ICommandBuilder<T> Usage(string message);

        ICommandBuilder<T> Description(string message);

        ICommandBuilder<T> Handler(CommandHandler handler);

        ICommandBuilder<T> Permission(UserPermission permission);

        ICommandBuilder<ICommandBuilder<T>> SubCommand(string name, AccessLevel level);
    }
}