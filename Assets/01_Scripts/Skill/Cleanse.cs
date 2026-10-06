using UnityEngine;

public class Cleanse : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    private PlayerStatusEffectController _playerStatusEffectController;

    private void Awake()
    {
        _playerStatusEffectController = GetComponent<PlayerStatusEffectController>();
    }

    private void OnEnable()
    {
        _playerInputHandler.Skill2Pressed += UseCleanse;
    }

    private void OnDisable()
    {
        _playerInputHandler.Skill2Pressed -= UseCleanse;
    }

    private void UseCleanse()
    {
        _playerStatusEffectController.ClearAllEffects();
    }
}