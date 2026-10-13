using UnityEngine;

public static class TutorialCanvasExtensions
{
    /// <summary>
    /// 캔버스 좌표 변환에 쓸 카메라를 돌려준다. 오버레이 캔버스는 카메라 없이(null) 변환한다.
    /// </summary>
    public static Camera GetUiCamera(this Canvas canvas)
    {
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }
}
