using UnityEngine;

public sealed class BgmRunner : MonoBehaviour
{
    private void Update()
    {
        Bgm.Tick(Time.unscaledDeltaTime);
    }
}