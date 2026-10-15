using UnityEngine.SceneManagement;
using static UnityEngine.SceneManagement.SceneManager;

public static class SceneLoader
{
    private const string CombatScene = "CombatScene";
    private const string TownScene = "TownScene";

    public static void LoadCombatSceneScene()
    {
        LoadSceneAsync(CombatScene, LoadSceneMode.Single);
    }

    public static void LoadTownScene()
    {
        LoadSceneAsync(TownScene, LoadSceneMode.Single);
    }
}