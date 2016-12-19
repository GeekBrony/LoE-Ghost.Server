using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Network;
using System;

namespace TestApp
{
    class Program
    {
        public class ConsoleUserIdentity : IUserIdentity
        {
            public AccessLevel Access
            {
                get
                {
                    return AccessLevel.TeamMember;
                }
            }

            public bool CheckPermission(UserPermission permission)
            {
                return true;
            }

            public void Info(string message)
            {
                Console.WriteLine(message);
            }

            public void Error(string message)
            {
                Console.WriteLine(message);
            }
        }

        static void Main(string[] args)
        {
            var user = new ConsoleUserIdentity();
            var manager = TestContext.CreateCommandManager();

            manager.Command("test", AccessLevel.Default)
                        .Handler((x, y, z) =>
                        {
                            y.Info("Handler: test");
                        })
                        .Description("helpful message!")
                        .SubCommand("all", AccessLevel.Player)
                            .Handler((x, y, z) =>
                            {
                                y.Info("Handler: test.all");
                            })
                            .Description("very helpful message!")
                            .SubCommand("all", AccessLevel.Admin)
                                .Handler((x, y, z) =>
                                {
                                    y.Info("Handler: test.all.all");
                                })
                                .Description("another very helpful message!")
                            .Previous()
                        .Previous()
                   .Previous()
                   .Command("exit", AccessLevel.Player)
                        .Handler((x, y, z) =>
                        {
                            x.IsRunning = false;
                        })
                        .Description("close application")
                   .Previous()
                   .Command("float", AccessLevel.Player)
                        .Handler((x, y, z) =>
                        {
                            if (z.TryGet(out float arg))
                                y.Info($"Hey! There is float {arg}");
                            else y.Info($"No float hear :(");
                        })
                        .Description("prints float or double")
                   .Previous();
            var testr = new MemoryPool(2, 32);
            var message = NetMessage.CreateNew();
            var ttt = message.ReadByte();
            ttt = message.ReadByte();
            ttt = message.ReadByte();
            ttt = message.ReadByte();
            message.Write((byte)0);
            message.Write((byte)0);
            message.Write((byte)0);
            message.Write((byte)0);


            do
            {
                Console.Write("$: ");
                manager.Execute(user, Console.ReadLine());

            } while (manager.IsRunning);
            Console.Write("Press ENTER to exit...");
            Console.ReadLine();
        }
    }
}