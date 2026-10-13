using UnityEngine;

/// <summary>
/// 중앙 SFX 카탈로그. 파일명 규칙으로 09_Audio 클립을 매핑한다.
/// SfxLibrary.asset 하나로 관리하고, 코드는 Sfx.Play(id)만 호출한다.
/// </summary>
[CreateAssetMenu(fileName = "SfxLibrary", menuName = "Audio/Sfx Library")]
public class SfxLibrary : ScriptableObject
{
    [Header("파일명 규칙 매핑 (09_Audio)")]
    public AudioClip RunePickedUp;
    public AudioClip Footsteps;
    public AudioClip EnemyDeath;
    public AudioClip EnemyHit;
    public AudioClip PlayerHit;
    public AudioClip ShieldInit;
    public AudioClip PortalMove;
    public AudioClip CleanseDone;
    public AudioClip ShieldHit;
    public AudioClip TornadoLoop;
    public AudioClip RewardShow;
    public AudioClip Reload;
    public AudioClip TraitSelect;
    public AudioClip Dash;
    public AudioClip CleanseSpell;
    public AudioClip MissionStart;
    public AudioClip ElementFire;
    public AudioClip ElementWater;
    public AudioClip ElementWind;
    public AudioClip ElementEarth;
    public AudioClip StatueBreak;
}
