using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCombatBaseStatsSO", menuName = "Scriptable Objects/PlayerCombatBaseStatsSO")]
public class PlayerCombatBaseStatsSO : ScriptableObject
{
    public float MaxHealth { get; private set; }
    public float MoveSpeed { get; private set; }
    public float FireInterval { get; private set; }
    public float BulletDamage { get; private set; }

    public PlayerCombatStats CreateStats()
    {
        return null;
    }
}