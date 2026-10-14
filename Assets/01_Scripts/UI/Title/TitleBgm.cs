using UnityEngine;

[DisallowMultipleComponent]
public class TitleBgm : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _fadeOutDurationSeconds = 2.5f;

    private void Start()
    {
        // 곡은 BgmLibrary에서 가져온다.
        BgmDirector.PlayZone(BgmDirector.Title);
    }

    public void FadeOut()
    {
        // 설정된 페이드아웃 시간으로 재생기에 페이드아웃을 요청한다.
        Bgm.FadeOut(_fadeOutDurationSeconds);
    }
}