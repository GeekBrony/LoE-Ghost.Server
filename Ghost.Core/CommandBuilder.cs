using DryIoc;
using Ghost.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ghost.Core
{
    internal class CommandAlias : ICommandHandler
    {
        private string m_name;
        private ICommandHandler m_handler;

        public bool IsAlias => true;

        public AccessLevel Access => m_handler.Access;

        public bool IsSubcommand => m_handler.IsSubcommand;

        public string Name => m_name;

        public string FullName => m_handler.FullName + ':' + m_name;

        public CommandAlias(string name, ICommandHandler handler)
        {
            m_name = name;
            m_handler = handler;
        }

        public bool CheckPermission(IUserIdentity user)
        {
            return m_handler.CheckPermission(user);
        }

        public void Execute(IUserIdentity user, CommandArgs args)
        {
            m_handler.Execute(user, args);
        }

        public bool IsSubcommandOf(ICommandHandler handler)
        {
            return m_handler.IsSubcommandOf(handler);
        }
    }

    internal class CommandBuilder : CommandBuilder<ICommandManager>
    {
        public CommandBuilder(string name, AccessLevel access, IContainer container)
            : base(name, access, container)
        {
            m_previous = m_manager;
        }
    }

    internal class CommandBuilder<TPrevious, T> : CommandBuilder<TPrevious>
        where TPrevious : ICommandBuilder<T>
    {
        public CommandBuilder(string name, AccessLevel access, IContainer container, TPrevious previous)
            : base(name, access, container)
        {
            m_parent = previous;
            m_previous = previous;
        }
    } 

    internal class CommandBuilder<T> : ICommandBuilder<T>, ICommandHandler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetSubCommandName(ICommandBuilder value, string name)
        {
            return value == null ? name : value.FullName + '.' + name;
        }

        protected T m_previous;
        protected ICommandBuilder m_parent;
        protected ICommandManager m_manager;

        private string m_name;
        private string m_usage;
        private string m_description;
        private AccessLevel m_access;
        private IContainer m_container;
        private CommandHandler m_handler;
        private HashSet<UserPermission> m_permissions;

        public string Name => m_name;

        public bool IsAlias => false;

        public AccessLevel Access => m_access;

        public ICommandManager Manager => m_manager;

        public bool IsSubcommand => m_parent != null;

        public string FullName => GetSubCommandName(m_parent, m_name);

        public CommandBuilder(string name, AccessLevel access, IContainer container)
        {
            m_name = name;
            m_access = access;
            m_container = container;
            m_manager = container.Resolve<ICommandManager>();
            m_permissions = new HashSet<UserPermission>();
        }

        public T Previous()
        {
            return m_previous;
        }

        public ICommandBuilder<T> Alias(string name)
        {
            var aliasName = GetSubCommandName(m_parent, name);
            if (m_container.IsRegistered<ICommandHandler>(aliasName))
                throw new InvalidOperationException($"Alias: {name}, already registered!");
            m_container.UseInstance<ICommandHandler>(new CommandAlias(name, this), true, false, aliasName);
            return this;
        }

        public ICommandBuilder<T> Handler(CommandHandler handler)
        {
            if (m_handler != null)
                throw new InvalidOperationException($"Command: {FullName}, handler already specified!");
            m_handler = handler;
            return this;
        }

        public ICommandBuilder<T> Usage(string message)
        {
            if (m_usage != null)
                throw new InvalidOperationException($"Command: {FullName}, usage already specified!");
            m_usage = message;
            return this;
        }

        public ICommandBuilder<T> Description(string message)
        {
            if (m_description != null)
                throw new InvalidOperationException($"Command: {FullName}, description already specified!");
            m_description = message;
            return this;
        }

        public ICommandBuilder<T> Permission(UserPermission permission)
        {
            m_permissions.Add(permission);
            return this;
        }

        public ICommandBuilder<ICommandBuilder<T>> SubCommand(string name, AccessLevel access)
        {
            if (name.HasWhiteSpace())
                throw new InvalidOperationException($"White space in name not allowed!");
            var subName = GetSubCommandName(this, name);
            if (m_container.IsRegistered<ICommandHandler>(subName))
                throw new InvalidOperationException($"SubCommand: {name}, already registered!");
            var builder = new CommandBuilder<ICommandBuilder<T>, T>(name, access, m_container, this);
            m_container.UseInstance<ICommandHandler>(builder, true, false, subName);
            return builder;
        }

        public bool CheckPermission(IUserIdentity user)
        {
            if (m_permissions.Count != 0)
            {
                foreach (var permission in m_permissions)
                    if (!user.CheckPermission(permission))
                        return false;
            }
            return true;
        }

        public bool IsSubcommandOf(ICommandHandler handler)
        {
            return ReferenceEquals(m_parent, handler);
        }

        public void Execute(IUserIdentity user, CommandArgs args)
        {
            string argument;
            if (args.TryPeek(out argument))
            {
                var subName = GetSubCommandName(this, argument);
                var subCommand = m_container.Resolve<ICommandHandler>(subName, IfUnresolved.ReturnDefault);
                if (subCommand != null)
                {
                    if (user.Access >= subCommand.Access && subCommand.CheckPermission(user))
                        subCommand.Execute(user, args.Skip(1));
                    else user.Error($"Access Denied: no permission to execute {subCommand.Name} subcommand");
                    return;
                }
                else if (argument == "?")
                {
                    if (m_usage != null) user.Info($"Usage: {m_usage}");
                    if (m_description != null) user.Info($"Description: {m_description}");
                    var subCommands = m_container.ResolveMany<ICommandHandler>()
                        .Where(x => x.IsSubcommand && x.IsSubcommandOf(this) && user.Access >= x.Access && x.CheckPermission(user));
                    if (subCommands.Any())
                    {
                        user.Info($"Subcommands:");
                        foreach (var item in subCommands)
                                user.Info(item.Name);
                    }
                    return;
                }
            }
            m_handler?.Invoke(m_manager, user, args);
        }
    }
}