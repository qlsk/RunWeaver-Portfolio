using UnityEngine;

/// <summary>
/// 층 이동 균열(포탈) 연출 설정. 플레이어 도착 연출(PortalArrival)이 Resources에서 불러 쓴다.
/// 층 이동은 화면만 어두워졌다 밝아지는 방식이다. 다음 층에서 플레이어 뒤에 열린 균열(_riftPrefab)이 있다가 닫힌다.
/// 균열 그림과 바위 문 연출은 프리팹 안의 Animator(포탈 프리팹과 같은 컨트롤러)가 한다.
/// </summary>
[CreateAssetMenu(menuName = "Rune Weaver/Portal Rift Frames", fileName = "PortalRiftFrames")]
public class PortalRiftFrames : ScriptableObject
{
    public const string ResourcePath = "PortalRiftFrames";

    [Tooltip("균열 애니메이션 컨트롤러 (Open → Idle → Close). 포탈 프리팹과 같은 것을 쓴다")]
    [SerializeField] private RuntimeAnimatorController _controller;

    [Tooltip("도착할 때와 포탈에 들어갈 때 남기는 균열 프리팹 (PortalRiftFx)")]
    [SerializeField] private GameObject _riftPrefab;

    [Tooltip("(쓰지 않음) 예전 균열 그림 머티리얼")]
    [SerializeField] private Material _material;

    [Tooltip("균열 가운데 (바닥 기준 높이)")]
    [SerializeField] private Vector2 _centerOffset = new(0f, 2.1f);

    public RuntimeAnimatorController Controller => _controller;
    public GameObject RiftPrefab => _riftPrefab;
    public Vector2 CenterOffset => _centerOffset;
}
