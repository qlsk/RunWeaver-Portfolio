using UnityEngine;

[DisallowMultipleComponent]
public class TitleBgm : MonoBehaviour
{
    [SerializeField] private AudioClip _bgmClip;
    [SerializeField, Min(0f)] private float _cooldownSeconds = 8f;
    [SerializeField, Min(0f)] private float _fadeInDurationSeconds = 1.5f;
    [SerializeField, Min(0f)] private float _fadeOutDurationSeconds = 2.5f;

    private void Start()
    {
        // 클립과 쿨타임, 페이드인 시간으로 재생을 요청한다.
        Bgm.Play(_bgmClip, _cooldownSeconds, _fadeInDurationSeconds);
    }

    public void FadeOut()
    {
        // 설정된 페이드아웃 시간으로 재생기에 페이드아웃을 요청한다.
        Bgm.FadeOut(_fadeOutDurationSeconds);
    }
}