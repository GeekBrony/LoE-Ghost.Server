using DryIoc;
using Ghost.Core.Utilities;
using System;
using System.Threading;

namespace Ghost.Core
{
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
        private IContainer m_container;
        private ApplicationState m_state;
        private ICommandManager m_commands;
        private ISettingsManager m_settings;

        public string Name
        {
            get; private set;
        }

        public bool IsRunning
        {
            get
            {
                return m_state == ApplicationState.Running;
            }
        }

        public ICommandManager Commands => m_commands;

        public ISettingsManager Settings => m_settings;

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
            where TManager : class
        {
            return m_container.Resolve<TManager>(IfUnresolved.ReturnDefault);
        }

        protected virtual void OnInitialize() { }

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
            OnInitialize();
            m_state = ApplicationState.Initialized;
        }

        private void RegisterDefaults()
        {
            m_container.Register<ICommandManager, CommandManager>(Reuse.Singleton);
            m_container.Register<ISettingsManager, SettingsManager>(Reuse.Singleton);
            m_commands = m_container.Resolve<ICommandManager>();
            m_settings = m_container.Resolve<ISettingsManager>();
        }

        private void ApplicationStart()
        {
            try
            {
                m_state = ApplicationState.Running;
                while (m_state == ApplicationState.Running)
                {
                    Console.Write("$: ");
                    m_commands.Execute(ConsoleUser.Default, Console.ReadLine());
                }
            }
            catch (ThreadAbortException)
            {

            }
            finally
            {
                m_state = ApplicationState.Exited;
            }
        }

        private void ExitCommand(ICommandManager manager, IUserIdentity user, CommandArgs args) => Exit();
    }
}