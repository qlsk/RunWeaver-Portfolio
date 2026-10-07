using UnityEngine;

/// <summary>
/// 에디터에서 전투 씬을 바로 실행했을 때를 위한 대비. 원정이 없으면 원정(지갑, 시작 골드)을 시작한다.
/// 정식 흐름에서는 마을에서 룬을 확정하고 출발할 때 RunFlow.StartExpedition으로 원정이 시작되므로
/// 이미 원정 중이라 아무것도 하지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class TemporaryExpeditionStarter : MonoBehaviour
{
    private void Update()
    {
        GameSession session = GameSession.Instance;
        if (session == null || !session.IsReady || session.Current == null)
        {
            return;
        }

        if (RunFlow.StartExpedition())
        {
            Debug.Log("[TemporaryExpeditionStarter] 마을을 거치지 않아 원정을 여기서 시작했습니다.", this);
        }

        enabled = false;
    }
}
