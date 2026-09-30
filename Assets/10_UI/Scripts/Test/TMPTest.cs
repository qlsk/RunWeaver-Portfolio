using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TMPTest : MonoBehaviour
{
    private TMP_Text _textComponent;

    [Header("Golden Shine Settings")]
    public Color BaseColor = new Color(0.85f, 0.65f, 0.13f, 1f); // 어두운 금색
    public Color ShineColor = new Color(1f, 0.95f, 0.6f, 1f);    // 밝게 빛나는 금색
    public float ShineSpeed = 3f;   // 빛이 이동하는 속도
    public float ShineWidth = 0.5f; // 빛나는 영역의 넓이 (파동 빈도)

    void Awake()
    {
        _textComponent = GetComponent<TMP_Text>();
    }

    void Update()
    {
        // 텍스트 메쉬 데이터를 강제로 업데이트하여 최신 정보를 가져옴
        _textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = _textComponent.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            // 공백이나 렌더링되지 않는 문자는 건너뜀
            if (!charInfo.isVisible) continue;

            // 머티리얼 인덱스와 정점 배열 가져오기
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

            // 시간과 글자 순서(i)를 이용해 일렁이는 파동(Sine) 값 계산 (-1 ~ 1 -> 0 ~ 1)
            float wave = Mathf.Sin(Time.time * ShineSpeed - (i * ShineWidth));
            float t = (wave + 1f) / 2f; // 0 ~ 1 사이 값으로 정규화

            // 기본색과 빛나는 색 사이를 t값에 따라 보간
            Color32 lerpedColor = Color.Lerp(BaseColor, ShineColor, t);

            // 글자 하나를 구성하는 4개의 정점(Vertex)에 계산된 색상 적용
            vertexColors[vertexIndex + 0] = lerpedColor; // 좌측 하단
            vertexColors[vertexIndex + 1] = lerpedColor; // 좌측 상단
            vertexColors[vertexIndex + 2] = lerpedColor; // 우측 상단
            vertexColors[vertexIndex + 3] = lerpedColor; // 우측 하단
        }

        // 변경된 색상 데이터를 실제 텍스트 컴포넌트에 반영
        _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}