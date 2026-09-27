using UnityEngine;

[System.Serializable]
public class RuneStoneIconData
{
    [SerializeField] private string _id;
    [SerializeField] private Sprite _icon;

    public string Id => _id;
    public Sprite Icon => _icon;
}
