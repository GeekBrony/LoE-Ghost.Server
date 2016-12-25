using DryIoc;
using Ghost.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ghost.Core
{
    public class ReplaceMeException : InvalidOperationException
    {
        public ReplaceMeException()
            : base("ToDo: Replace me with more useful exception!")
        {

        }
    }

    public interface IGhostApplication
    {
        string Name
        {
            get;
        }

        bool IsRunning
        {
            get;
        }

        void Exit();

        TManager Resolve<TManager>()
            where TManager : IGhostManager;
    }

    public enum ApplicationState
    {
        None,
        Initializing,
        Initialized,
        Starting,
        Running,
        Exiting,
        Exited
    }

    public interface IGhostManager
    {
        IGhostApplication Application { get; }

        void Configure();

        void Initialize();

        void Startup();

        void Exit();
    }

    public interface IUsersManager : IGhostManager
    {
        IUserIdentity ConsoleIdentity
        {
            get;
        }
    }

    internal class UsersManager : IUsersManager
    {
        private class ConsoleUser : IUserIdentity
        {
            public static readonly ConsoleUser Default = new ConsoleUser();

            public AccessLevel Access => AccessLevel.Admin;

            public bool CheckPermission(UserPermission permission)
            {
                return true;
            }

            public void Error(string message)
            {
                Console.WriteLine($"Error[{DateTime.Now}]: {message}");
            }

            public void Info(string message)
            {
                Console.WriteLine($"Info [{DateTime.Now}]: {message}");
            }
        }

        public IGhostApplication Application
        {
            get; private set;
        }

        public IUserIdentity ConsoleIdentity => ConsoleUser.Default;

        public UsersManager(IGhostApplication application)
        {
            Application = application;
        }

        public void Configure()
        {
            //throw new NotImplementedException();
        }

        public void Initialize()
        {
            //throw new NotImplementedException();
        }

        public void Exit()
        {
            //throw new NotImplementedException();
        }

        public void Startup()
        {
            //throw new NotImplementedException();
        }
    }

    public abstract class GhostApplication : IGhostApplication
    {
        private static readonly object s_lock = new object();

        public static GhostApplication Current
        {
            get; private set;
        }

        public static void Startup<TApplication>()
            where TApplication : GhostApplication
        {
            lock (s_lock)
            {
                if (Current?.IsRunning ?? false)
                    throw new ReplaceMeException();
                Current = ReflectionExt.New<TApplication>.Create();
                Current.Startup();
            }
        }

        private Thread m_thread;
        private IUsersManager m_users;
        private IContainer m_container;
        private ApplicationState m_state;
        private ICommandManager m_commands;

        public string Name
        {
            get; private set;
        }

        public bool IsRunning
        {
            get => m_state == ApplicationState.Running;
        }

        public IUsersManager Users => m_users;

        public ICommandManager Commands => m_commands;


        public GhostApplication(string name)
        {
            Name = name;
            Console.Title = name;
            m_thread = new Thread(ApplicationStart)
            {
                Name = $"GhostApp[{name}]",
            };
            m_container = new Container();
            m_container.UseInstance<IGhostApplication>(this, true);
        }

        public void Exit()
        {
            if (m_state > ApplicationState.Initialized && m_state < ApplicationState.Exiting)
            {
                m_state = ApplicationState.Exiting;
                m_thread.Abort();
            }
        }

        public TManager Resolve<TManager>() 
            where TManager : IGhostManager
        {
            return m_container.Resolve<TManager>(IfUnresolved.ReturnDefault);
        }

        private void Startup()
        {
            if (m_state < ApplicationState.Initialized)
                Initialize();
            m_state = ApplicationState.Starting;
            m_thread.Start();
        }

        private void Initialize()
        {
            m_state = ApplicationState.Initializing;
            RegisterDefaults();
            m_commands.Command("exit", AccessLevel.Admin)
                .Description("Exit application")
                .Handler(ExitCommand);
            foreach (var item in m_container.ResolveMany<IGhostManager>())
                item.Initialize();
            m_state = ApplicationState.Initialized;
        }

        private void RegisterDefaults()
        {
            m_container.Register<IUsersManager, UsersManager>(Reuse.Singleton);
            m_container.Register<ICommandManager, CommandManager>(Reuse.Singleton);
            m_users = m_container.Resolve<IUsersManager>();
            m_commands = m_container.Resolve<ICommandManager>();
        }

        private void ApplicationStart()
        {
            try
            {
                foreach (var item in m_container.ResolveMany<IGhostManager>())
                    item.Startup();
                m_state = ApplicationState.Running;
                while (m_state == ApplicationState.Running)
                {
                    Console.Write("$: ");
                    m_commands.Execute(m_users.ConsoleIdentity, Console.ReadLine());
                }
            }
            catch (ThreadAbortException)
            {

            }
            finally
            {
                foreach (var item in m_container.ResolveMany<IGhostManager>())
                    item.Exit();
                m_state = ApplicationState.Exited;
            }
        }

        private static void ExitCommand(ICommandManager manager, IUserIdentity user, CommandArgs args) => manager.Application.Exit();
    }
}