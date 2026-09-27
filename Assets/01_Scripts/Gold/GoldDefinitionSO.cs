using UnityEngine;

[CreateAssetMenu(fileName = "GoldDefinitionSO", menuName = "Scriptable Objects/GoldDefinitionSO")]
public class GoldDefinitionSO : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;

    public string Id => _id;
    public string DisplayName => _displayName;
}
