using DryIoc;

namespace Ghost.Core
{
    public static class TestContext
    {
        public static ICommandManager CreateCommandManager()
        {
            return new CommandManager(new Container());
        }
    }
}
