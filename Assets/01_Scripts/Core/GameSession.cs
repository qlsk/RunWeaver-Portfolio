using System;

public class GameSession : Singleton<GameSession>
{
    private GameDefinitionSet _definitions;
    private PlayerProgression _current;
    private bool _isReady;

    public PlayerProgression Current => _current;
    public bool IsReady => _isReady;
    public event Action<PlayerProgression> ProgressReplaced;

    public void Initialize(GameDefinitionSet definitions)
    {
        if (_isReady) throw new InvalidOperationException("GameSession already initialized");
        if (definitions == null) throw new ArgumentNullException(nameof(definitions));
        PlayerProgression progression = new(definitions);
        _definitions = definitions;
        _current = progression;
        _isReady = true;
        ProgressReplaced?.Invoke(progression);
    }

    public void StartNewGame()
    {
        if (!_isReady) throw new InvalidOperationException("GameSession is not ready");
        PlayerProgression progression = new(_definitions);
        _current = progression;
        ProgressReplaced?.Invoke(progression);
    }
}
