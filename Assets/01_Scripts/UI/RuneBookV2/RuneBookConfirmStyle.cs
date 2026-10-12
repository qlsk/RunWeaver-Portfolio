using TMPro;
using UnityEngine;

/// <summary>
/// 룬북 확정 확인 창(RuneBookConfirmDialog)이 쓰는 그림과 글꼴 묶음. Resources/RuneBookConfirmStyle에 둔다.
/// 룬북 기록 질감(쪽지 종이, 핀, 문장, 구분선, 밀랍 인장)을 그대로 쓴다.
/// </summary>
[CreateAssetMenu(menuName = "Rune Weaver/Rune Book Confirm Style", fileName = "RuneBookConfirmStyle")]
public class RuneBookConfirmStyle : ScriptableObject
{
    public const string ResourcePath = "RuneBookConfirmStyle";

    public Sprite Paper;
    public Sprite Pin;
    public Sprite Emblem;
    public Sprite Divider;
    public Sprite Seal;
    public TMP_FontAsset Font;
}
