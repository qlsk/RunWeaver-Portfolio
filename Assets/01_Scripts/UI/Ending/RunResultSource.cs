using UnityEngine;

/// <summary>
/// 결과창이 읽는 한 판의 결과. 현재는 결과를 집계하는 코드가 없으므로,
/// 집계 기능이 생기면 이 클래스를 상속해 실제 결과를 넘기는 어댑터를 만든다.
/// </summary>
public abstract class RunResultSource : MonoBehaviour
{
    public abstract RunResultData GetResult();
}
