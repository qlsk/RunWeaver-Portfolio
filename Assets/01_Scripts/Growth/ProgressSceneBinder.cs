using System;
using UnityEngine;

public class ProgressSceneBinder : MonoBehaviour
{
    private GameSession _session;
    private PlayerProgression _boundProgress;

    private void OnEnable()
    {
        _session = GameSession.Instance;
        if (_session == null)
        {
            throw new InvalidOperationException("Bootstrap did not execute.");
        }
        _boundProgress = _session.Current;
        if (_session.IsReady)
        {
            Bind(_boundProgress);
        }
        _session.ProgressReplaced += Bind;
    }

    private void OnDisable()
    {
        if (_session != null)
        {
            _session.ProgressReplaced -= Bind;
        }
        Unbind();
    }

    public void Bind(PlayerProgression progress)
    {
        if (progress == null)
        {
            throw new ArgumentNullException(nameof(progress));
        }
        if (_boundProgress != null)
        {
            Unbind();
        }
        // TODO: 추후 사용처 구현 시 전달
        _boundProgress = progress;
    }

    public void Unbind()
    {
        // TODO: 추후 사용처 구현 시 해제
        _boundProgress = null;
    }
}
