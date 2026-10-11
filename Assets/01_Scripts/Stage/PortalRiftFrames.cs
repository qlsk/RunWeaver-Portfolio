using UnityEngine;
using UnityEngine.Serialization;

/// <summary>층 이동 연출 방식</summary>
public enum PortalTransitionStyle
{
    /// <summary>플레이어는 그대로 두고 화면만 어두워졌다 밝아진다. 다음 층에서 열린 균열 앞에 서 있다가 균열이 닫힌다.</summary>
    Fade,
    /// <summary>플레이어가 균열로 빨려 들어가고, 다음 층에서 균열이 플레이어를 뱉어낸다.</summary>
    Rift
}

/// <summary>
/// 공중의 균열(포탈) 도착 연출 설정. 플레이어 도착 연출(PortalArrival)이 Resources에서 불러 쓴다.
/// 균열 그림은 Controller(Open → Idle → Close 애니메이션)가 바꾼다. 포탈 프리팹(Portal)과 같은 컨트롤러를 쓴다.
/// </summary>
[CreateAssetMenu(menuName = "Rune Weaver/Portal Rift Frames", fileName = "PortalRiftFrames")]
public class PortalRiftFrames : ScriptableObject
{
    public const string ResourcePath = "PortalRiftFrames";

    [Tooltip("층 이동 연출. Fade : 화면만 어두워졌다 밝아진다 / Rift : 균열로 빨려 들어가고 뱉어낸다")]
    [SerializeField, FormerlySerializedAs("Style")] private PortalTransitionStyle _style = PortalTransitionStyle.Fade;

    [Tooltip("균열 애니메이션 컨트롤러 (Open → Idle → Close). 포탈 프리팹과 같은 것을 쓴다")]
    [SerializeField] private RuntimeAnimatorController _controller;

    [Tooltip("균열 그림에 쓸 머티리얼 (포탈 프리팹과 같게). 비우면 기본 스프라이트 머티리얼")]
    [SerializeField, FormerlySerializedAs("Material")] private Material _material;

    [Tooltip("균열 가운데 (바닥 기준 높이)")]
    [SerializeField, FormerlySerializedAs("CenterOffset")] private Vector2 _centerOffset = new(0f, 2.1f);

    public PortalTransitionStyle Style => _style;
    public RuntimeAnimatorController Controller => _controller;
    public Material Material => _material;
    public Vector2 CenterOffset => _centerOffset;
}
