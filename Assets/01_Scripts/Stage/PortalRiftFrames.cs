using UnityEngine;

/// <summary>
/// 공중의 균열(포탈) 프레임 묶음. 플레이어 도착 연출(PortalArrival)이 Resources에서 불러 쓴다.
/// 포탈 프리팹(Portal)과 같은 스프라이트를 쓴다.
/// </summary>
/// <summary>층 이동 연출 방식</summary>
public enum PortalTransitionStyle
{
    /// <summary>플레이어는 그대로 두고 화면만 어두워졌다 밝아진다. 다음 층에서 열린 균열 앞에 서 있다가 균열이 닫힌다.</summary>
    Fade,
    /// <summary>플레이어가 균열로 빨려 들어가고, 다음 층에서 균열이 플레이어를 뱉어낸다.</summary>
    Rift
}

[CreateAssetMenu(menuName = "Rune Weaver/Portal Rift Frames", fileName = "PortalRiftFrames")]
public class PortalRiftFrames : ScriptableObject
{
    public const string ResourcePath = "PortalRiftFrames";

    [Tooltip("층 이동 연출. Fade : 화면만 어두워졌다 밝아진다 / Rift : 균열로 빨려 들어가고 뱉어낸다")]
    public PortalTransitionStyle Style = PortalTransitionStyle.Fade;

    public Sprite[] OpenFrames;
    public Sprite[] IdleFrames;
    public Sprite[] CloseFrames;
    [Tooltip("균열 그림에 쓸 머티리얼 (포탈 프리팹과 같게). 비우면 기본 스프라이트 머티리얼")]
    public Material Material;
    [Tooltip("균열 가운데 (바닥 기준 높이)")]
    public Vector2 CenterOffset = new(0f, 2.1f);
}
