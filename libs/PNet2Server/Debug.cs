using System;
using PNet;

namespace PNetS
{
    /// <summary>
    /// Debug
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// Reference to the actual log receiver
        /// </summary>
        public static ILogger Logger = new NullLogger();

        /// <summary>
        /// Info message
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        [JetBrains.Annotations.StringFormatMethod("value")]
        public static void Log(string value, params object[] args)
        {
            Logger.Info(value, args);
        }
        /// <summary>
        /// Error message
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        [JetBrains.Annotations.StringFormatMethod("value")]
        public static void LogError(string value, params object[] args)
        {
            Logger.Error(value, args);
        }
        /// <summary>
        /// Warning message
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        [JetBrains.Annotations.StringFormatMethod("value")]
        public static void LogWarning(string value, params object[] args)
        {
            Logger.Warning(value, args);
        }

        /// <summary>
        /// exception
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="value"></param>
        /// <param name="args"></param>
        [JetBrains.Annotations.StringFormatMethod("value")]
        public static void LogException(Exception exception, string value, params object[] args)
        {
            Logger.Exception(exception, value, args);
        }

        /// <summary>
        /// exception
        /// </summary>
        /// <param name="exception"></param>
        public static void LogException(Exception exception)
        {
            Logger.Exception(exception, "");
        }
    }
}
