using System;

namespace PNetR.Utils
{
    static class DelegateExtensions
    {
        public static void SafeRaise<T>(this Action<T> eventHandler, T arg)
        {
            if (eventHandler == null) return;
            try
            {
                eventHandler(arg);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        public static void SafeRaise<T1, T2>(this Action<T1, T2> eventHandler, T1 arg1, T2 arg2)
        {
            if (eventHandler == null) return;
            try
            {
                eventHandler(arg1, arg2);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
