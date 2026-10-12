using UnityEngine;

/// <summary>
/// 임무 실패 연출에서 발밑에 퍼지는 오염 줄기. RunFailureSequence가 프리팹(FailureVeinsFx)으로 만든다.
/// 퍼지는 정도(_reveal)와 투명도는 애니메이션 클립(FailureVeins_Play)이 움직이고,
/// 이 스크립트는 그 값을 셰이더의 드러남 반지름(월드 좌표)으로 옮겨 준다.
/// </summary>
[DisallowMultipleComponent]
public class FailureVeinsFx : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [Tooltip("퍼진 정도 (0 → 1). 애니메이션 클립이 움직인다.")]
    [SerializeField] private float _reveal;
    [Tooltip("다 퍼졌을 때의 반지름 (캐릭터 키 배율)")]
    [SerializeField, Min(0f)] private float _radiusScale = 2.86f;
    [Tooltip("경계가 부드러운 폭 (캐릭터 키 배율)")]
    [SerializeField, Min(0f)] private float _softnessScale = 0.35f;
    [Tooltip("바닥에 눕힌 비율 (세로 / 가로). 프리팹의 Holder 세로 크기와 맞춘다.")]
    [SerializeField, Min(0.01f)] private float _squash = 0.5f;

    private Material _material;
    private float _height = 1f;

    /// <summary>발 위치와 캐릭터 키에 맞춘다. (크기는 키 배율로 바뀐다)</summary>
    public void Setup(Vector3 feet, float height)
    {
        _height = Mathf.Max(0.01f, height);
        transform.position = feet;
        transform.localScale = new Vector3(_height, _height, 1f);

        if (_renderer == null)
        {
            return;
        }

        _material = _renderer.material;
        _material.SetVector(FailureSpriteMaterial.RevealCenter, new Vector4(feet.x, feet.y, 1f, 1f / _squash));
        _material.SetFloat(FailureSpriteMaterial.RevealSoftness, _height * _softnessScale);
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void Apply()
    {
        if (_material != null)
        {
            _material.SetFloat(FailureSpriteMaterial.RevealRadius, _reveal * _radiusScale * _height);
        }
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
        }
    }
}
