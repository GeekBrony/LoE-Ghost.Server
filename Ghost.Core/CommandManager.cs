using DryIoc;
using Ghost.Core.Utilities;
using System;
using System.Linq;

namespace Ghost.Core
{
    internal class CommandManager : ICommandManager
    {
        public bool IsRunning
        {
            get; set;
        }

        public IContainer Container
        {
            get; private set;
        }

        public CommandManager(IContainer container)
        {
            IsRunning = true;
            Container = container;
        }

        public void Execute(IUserIdentity user, string message)
        {
            if (user.CheckPermission(UserPermission.ExecuteCommands))
            {
                if (message == "?")
                {
                    var commands = Container.ResolveMany<ICommandHandler>()
                        .Where(x => !x.IsSubcommand && user.Access >= x.Access && x.CheckPermission(user));
                    if (commands.Any())
                    {
                        user.Info("Available commands:");
                        foreach (var item in commands)
                            user.Info(item.Name);
                    }
                    else user.Info("No available commands");
                    return;
                }
                else
                {
                    var args = new CommandArgs(message);
                    var command = Container.Resolve<ICommandHandler>(args.Command, IfUnresolved.ReturnDefault);
                    if (command != null)
                    {
                        if (command.Access <= user.Access && command.CheckPermission(user))
                            command.Execute(user, args);
                        else user.Error($"Access Denied: no permission to execute {args.Command} command");
                    }
                    else user.Error($"Command: {args.Command}, not found");
                }
            }
            else user.Error($"Access Denied: no permission to execute commands");
        }

        public ICommandBuilder<ICommandManager> Command(string name, AccessLevel access)
        {
            if (name.HasWhiteSpace())
                throw new InvalidOperationException($"White space in name not allowed!");
            if (Container.IsRegistered<ICommandHandler>(name))
                throw new InvalidOperationException($"Command: {name}, already registered!");
            var builder = new CommandBuilder(name, access, this);
            Container.UseInstance<ICommandHandler>(builder, true, false, name);
            return builder;
        }
    }
}