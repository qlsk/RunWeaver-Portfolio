using UnityEngine;

/// <summary>
/// 공중의 균열(포탈) 프레임 묶음. 플레이어 도착 연출(PortalArrival)이 Resources에서 불러 쓴다.
/// 포탈 프리팹(Portal)과 같은 스프라이트를 쓴다.
/// </summary>
[CreateAssetMenu(menuName = "Rune Weaver/Portal Rift Frames", fileName = "PortalRiftFrames")]
public class PortalRiftFrames : ScriptableObject
{
    public const string ResourcePath = "PortalRiftFrames";

    public Sprite[] OpenFrames;
    public Sprite[] IdleFrames;
    public Sprite[] CloseFrames;
    [Tooltip("균열 그림에 쓸 머티리얼 (포탈 프리팹과 같게). 비우면 기본 스프라이트 머티리얼")]
    public Material Material;
    [Tooltip("균열 가운데 (바닥 기준 높이)")]
    public Vector2 CenterOffset = new(0f, 2.1f);
}
