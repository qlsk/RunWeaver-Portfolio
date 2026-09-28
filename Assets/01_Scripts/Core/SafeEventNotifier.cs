using System;
using UnityEngine;

public static class SafeEventNotifier
{
    public static void Notify(Action handlers)
    {
        if (handlers == null) return;

        foreach (Delegate action in handlers.GetInvocationList())
        {
            var handler = (Action)action;
            try
            {
                handler();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    public static void Notify<T>(Action<T> handlers, T value)
    {
        if (handlers == null) return;

        foreach (Delegate action in handlers.GetInvocationList())
        {
            var handler = (Action<T>)action;
            try
            {
                handler(value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
