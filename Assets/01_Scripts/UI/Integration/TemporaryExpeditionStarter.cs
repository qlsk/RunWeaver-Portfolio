using UnityEngine;

/// <summary>
/// [임시] 전투 씬을 단독으로 실행할 때 원정을 시작한다. (원정 지갑, 원정 자원이 생겨야 골드 HUD와 결과창이 동작한다)
/// 로비에서 원정을 시작하는 흐름이 생기면 이 컴포넌트를 제거한다.
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

        if (!session.Current.Holdings.IsOnExpedition)
        {
            session.StartExpedition();
            Debug.Log("[TemporaryExpeditionStarter] 원정을 시작했습니다. (임시)", this);
        }

        enabled = false;
    }
}
