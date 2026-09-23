using System;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    [SerializeField] private bool _isDontDestroyOnLoad = true;

    private static T s_instance;
    public static T Instance => s_instance;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this as T;
        if (_isDontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        OnAwake();
    }

    protected virtual void OnAwake() { }

    private void OnDestroy()
    {
        if (s_instance != this)
        {
            return;
        }

        OnSingletonDestroyed();
        s_instance = null;
    }

    protected virtual void OnSingletonDestroyed() { }
}
