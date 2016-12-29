using Ghost.Core.Utilities;
using System;

namespace Ghost.Core
{
    internal class ConsoleUser : IUserIdentity
    {
        public static readonly ConsoleUser Default = new ConsoleUser();

        public string Name => "Console";

        public AccessLevel Access => AccessLevel.Admin;

        public bool CheckPermission(UserPermission permission)
        {
            return true;
        }

        public void Error(string message)
        {
            Console.WriteLine(message);
        }

        public void Info(string message)
        {
            Console.WriteLine(message);
        }
    }
}