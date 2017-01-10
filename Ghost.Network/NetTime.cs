using System.Diagnostics;

namespace Ghost.Network
{
    public static class NetTime
    {
        private static readonly long s_timeInitialized = Stopwatch.GetTimestamp();
        private static readonly double s_dInvFreq = 1.0d / Stopwatch.Frequency;

        /// <summary>
        /// Get number of seconds since the application started
        /// </summary>
        public static double Now => (Stopwatch.GetTimestamp() - s_timeInitialized) * s_dInvFreq;
    }
}