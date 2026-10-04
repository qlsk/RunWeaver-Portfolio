using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const string SpritRealmScene = "SpiritRealmScene";

    public static AsyncOperation LoadSpiritRealmScene()
    {
        return SceneManager.LoadSceneAsync(SpritRealmScene, LoadSceneMode.Single);
    }
}