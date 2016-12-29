namespace Ghost.Core
{
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
            where TManager : class;
    }
}