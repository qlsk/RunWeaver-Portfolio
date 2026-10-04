using UnityEngine.SceneManagement;
using static UnityEngine.SceneManagement.SceneManager;

public static class SceneLoader
{
    private const string CombatScene = "CombatScene";

    public static void LoadCombatSceneScene()
    {
        LoadSceneAsync(CombatScene, LoadSceneMode.Single);
    }
}