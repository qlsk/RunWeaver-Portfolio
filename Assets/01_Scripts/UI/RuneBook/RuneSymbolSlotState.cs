/// <summary>
/// 룬 문자 칸의 표시 상태
/// </summary>
public enum RuneSymbolSlotState
{
    /// <summary>잠긴 칸 (공개되지 않은 룬, 아직 열리지 않은 활성 칸)</summary>
    Locked,
    /// <summary>비어 있는 활성 칸</summary>
    Empty,
    /// <summary>목록만 공개된 룬</summary>
    Listed,
    /// <summary>상세까지 공개된 룬</summary>
    Detailed
}
