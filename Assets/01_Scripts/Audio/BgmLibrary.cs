using UnityEngine;

/// <summary>
/// BGM 슬롯 카탈로그. 상황별 곡을 BgmLibrary.asset 하나로 관리한다.
/// 코드는 BgmDirector.PlayZone(id)만 호출한다. 클립 파일은 09_Audio/BGM/에 둔다.
/// </summary>
[CreateAssetMenu(fileName = "BgmLibrary", menuName = "Audio/Bgm Library")]
public class BgmLibrary : ScriptableObject
{
    [Header("상황별 BGM (09_Audio/BGM)")]
    public AudioClip Title;
    public AudioClip Combat;
    public AudioClip Boss;
    public AudioClip Shop;
    public AudioClip ResultClear;
    public AudioClip ResultFailure;
}
