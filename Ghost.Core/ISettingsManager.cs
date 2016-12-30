namespace Ghost.Core
{
    public interface ISettingsManager
    {
        void Save();

        void Reload();

        T Get<T>(string name);

        void Set<T>(string name, T value);
    }
}