using UnityEngine;

[CreateAssetMenu(fileName = "ResourceDefinitionSO", menuName = "Scriptable Objects/ResourceDefinitionSO")]
public class ResourceDefinitionSO : ScriptableObject
{
    [SerializeField] string _id;
    [SerializeField] string _displayName;
    [SerializeField] string _description;
    [SerializeField] Sprite _icon;

    public string Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
}
