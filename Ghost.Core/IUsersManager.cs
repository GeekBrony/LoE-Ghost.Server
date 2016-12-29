namespace Ghost.Core
{
    public interface IUsersManager
    {
        bool TryGetById(int id, out IUserIdentity user);

        bool TryGetByName(string name, out IUserIdentity user);
    }
}