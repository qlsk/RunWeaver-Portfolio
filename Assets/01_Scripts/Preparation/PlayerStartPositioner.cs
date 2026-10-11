using UnityEngine;

/// <summary>
/// 전투 씬이 열릴 때 플레이어를 시작 위치에 놓는다.
/// 기준 위치(_anchor, 비워두면 이 오브젝트)에서 _startOffset만큼 떨어진 곳이 시작 위치다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerStartPositioner : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [Tooltip("시작 위치의 기준. 비워두면 이 오브젝트")]
    [SerializeField] private Transform _anchor;
    [Tooltip("씬이 열릴 때 플레이어를 놓을 위치 (기준 위치 기준)")]
    [SerializeField] private Vector3 _startOffset = new(0f, -3f, 0f);

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
        if (player == null)
        {
            return;
        }

        Transform anchor = _anchor != null ? _anchor : transform;
        player.transform.position = anchor.position + _startOffset;
    }
}
