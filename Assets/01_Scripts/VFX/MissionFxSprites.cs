using UnityEngine;

/// <summary>
/// 미션 오브젝트 연출이 같이 쓰는 코드로 만든 그림과 머티리얼. (이미지 에셋 없이 처음 쓸 때 한 번 만든다)
/// - SoftCircle : 가운데가 진하고 가장자리로 갈수록 투명한 원 (후광). 1 x 1 단위 크기.
/// - Pixel : 흰색 한 칸. 1 x 1 단위 크기. (빛 알갱이)
/// - UnlitMaterial : 조명을 받지 않는 스프라이트 머티리얼. 찾지 못하면 null (기본 머티리얼을 쓴다)
/// </summary>
public static class MissionFxSprites
{
    private static Sprite s_softCircle;
    private static Sprite s_pixel;
    private static Material s_unlit;
    private static bool s_searchedUnlit;

    public static Sprite SoftCircle
    {
        get
        {
            if (s_softCircle == null)
            {
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2f - 1f;
                        float dy = (y + 0.5f) / size * 2f - 1f;
                        float a = Mathf.Exp(-(dx * dx + dy * dy) * 4f) * Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }

                texture.Apply();
                s_softCircle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }

            return s_softCircle;
        }
    }

    public static Sprite Pixel
    {
        get
        {
            if (s_pixel == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                s_pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
            }

            return s_pixel;
        }
    }

    public static Material UnlitMaterial
    {
        get
        {
            if (!s_searchedUnlit)
            {
                s_searchedUnlit = true;
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader != null)
                {
                    s_unlit = new Material(shader) { name = "MissionFx (Unlit)" };
                }
            }

            return s_unlit;
        }
    }
}
