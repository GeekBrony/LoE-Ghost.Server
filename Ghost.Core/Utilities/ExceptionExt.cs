using System;

namespace Ghost.Core.Utilities
{
    public class ReplaceMeException : InvalidOperationException
    {
        public ReplaceMeException()
            : base("ToDo: Replace me with more useful exception!")
        {

        }
    }

    public static class ExceptionExt
    {
    }
}