using System;

namespace PNet
{
    /// <summary>
    /// Console recipient for the log
    /// </summary>
    public sealed class DefaultConsoleLogger : ILogger
    {
        /// <summary>
        /// Info
        /// </summary>
        /// <param name="info"></param>
        /// <param name="args"></param>
        public void Info(string info, params object[] args)
        {
            Console.WriteLine(info, args);
        }

        /// <summary>
        /// Warning
        /// </summary>
        /// <param name="info"></param>
        /// <param name="args"></param>
        public void Warning(string info, params object[] args)
        {
            Console.WriteLine(info, args);
        }

        /// <summary>
        /// error
        /// </summary>
        /// <param name="info"></param>
        /// <param name="args"></param>
        public void Error(string info, params object[] args)
        {
            Console.WriteLine(info, args);
        }

        /// <summary>
        /// exception
        /// </summary>
        /// <param name="info"></param>
        /// <param name="exception"></param>
        /// <param name="args"></param>
        public void Exception(Exception exception, string info, params object[] args)
        {
            Console.WriteLine(string.Format(info, args) + " ex: {0}", exception);
        }
    }
}
