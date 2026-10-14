using UnityEngine;

/// <summary>
/// 게임 마우스 커서. 평소에는 Idle 그림, 왼쪽 버튼을 누르면 Press 그림들이 차례로 나오고 마지막 그림에서 멈춘다.
/// 떼면 Release 그림들을 거쳐 Idle로 돌아온다.
/// - 프리팹(Resources/Cursor/GameCursor)에서 그림과 시간, 기준점(Hotspot)을 정한다. 씬에 둘 필요는 없다.
///   게임이 시작되면 스스로 만들어지고 씬이 바뀌어도 남는다.
/// - 그림은 텍스처 형식 Cursor (Read/Write 켬)로 가져온다.
/// - 일시정지 중에도 동작하도록 실제 시간으로 흐른다.
/// </summary>
[DisallowMultipleComponent]
public class GameCursor : MonoBehaviour
{
    private const string ResourcePath = "Cursor/GameCursor";

    [Header("그림")]
    [Tooltip("아무것도 누르지 않았을 때")]
    [SerializeField] private Texture2D _idle;
    [Tooltip("누르는 동안 차례로 보여 줄 그림. 마지막 그림에서 멈춘다")]
    [SerializeField] private Texture2D[] _pressFrames;
    [Tooltip("뗄 때 차례로 보여 줄 그림. 끝나면 Idle로 돌아간다")]
    [SerializeField] private Texture2D[] _releaseFrames;

    [Header("설정")]
    [Tooltip("그림 한 장이 보이는 시간 (초)")]
    [SerializeField, Min(0.01f)] private float _frameDuration = 0.04f;
    [Tooltip("커서가 가리키는 지점 (그림의 왼쪽 위 기준 픽셀). 십자 커서라 가운데")]
    [SerializeField] private Vector2 _hotspot = new(32f, 32f);
    [Tooltip("Auto는 하드웨어 커서. 커서가 보이지 않으면 ForceSoftware로 바꾼다")]
    [SerializeField] private CursorMode _cursorMode = CursorMode.Auto;

    private static GameCursor s_instance;

    private Texture2D[] _currentFrames;
    private int _frameIndex;
    private float _frameTimer;
    private bool _isPressed;
    private Texture2D _shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOnStart()
    {
        if (s_instance != null)
        {
            return;
        }

        GameCursor prefab = Resources.Load<GameCursor>(ResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[GameCursor] Resources/{ResourcePath} 프리팹이 없어 기본 커서를 씁니다.");
            return;
        }

        GameCursor cursor = Instantiate(prefab);
        cursor.name = prefab.name;
        DontDestroyOnLoad(cursor.gameObject);
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        Show(_idle);
    }

    private void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // 창을 다시 잡으면 운영체제가 커서를 되돌리는 경우가 있어 다시 건다.
        if (hasFocus && _shown != null)
        {
            Cursor.SetCursor(_shown, _hotspot, _cursorMode);
        }
    }

    private void Update()
    {
        bool isPressed = Input.GetMouseButton(0);
        if (isPressed != _isPressed)
        {
            _isPressed = isPressed;
            Play(isPressed ? _pressFrames : _releaseFrames);
        }

        if (_currentFrames == null)
        {
            return;
        }

        _frameTimer += Time.unscaledDeltaTime;
        if (_frameTimer < _frameDuration)
        {
            return;
        }

        _frameTimer = 0f;
        _frameIndex++;
        if (_frameIndex < _currentFrames.Length)
        {
            Show(_currentFrames[_frameIndex]);
            return;
        }

        // 누르는 중이면 마지막 그림에서 멈추고, 뗐으면 Idle로 돌아간다.
        _currentFrames = null;
        if (!_isPressed)
        {
            Show(_idle);
        }
    }

    private void Play(Texture2D[] frames)
    {
        _frameTimer = 0f;
        _frameIndex = 0;
        if (frames == null || frames.Length == 0)
        {
            _currentFrames = null;
            Show(_idle);
            return;
        }

        _currentFrames = frames;
        Show(frames[0]);
    }

    private void Show(Texture2D texture)
    {
        if (texture == null || texture == _shown)
        {
            return;
        }

        _shown = texture;
        Cursor.SetCursor(texture, _hotspot, _cursorMode);
    }
}
