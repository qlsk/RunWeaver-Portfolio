using System;
using UnityEngine.UI;

/// <summary>
/// UI팀이 직접 만든 카드 1장의 연결 슬롯.
/// 카드 루트에 Button, 자식으로 Icon/Name/Desc를 두고 드래그로 연결한다.
/// </summary>
[Serializable]
public struct RewardCardView
{
    public Image Background;
    public Button Button;
    public Image Icon;
    public Text Name;
    public Text Desc;

    /// <summary>카테고리 표시 텍스트 (선택 사항, 비우면 표시 생략).</summary>
    public Text Category;

    public bool IsValid =>
        Background != null &&
        Button != null &&
        Icon != null &&
        Name != null &&
        Desc != null;
}
