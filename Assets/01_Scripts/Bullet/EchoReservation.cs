public struct EchoReservation
{
    public float RemainingSeconds;
    public readonly EnemyHealth Target;
    public ReactionContext Context;

    public EchoReservation(float remainingSeconds, EnemyHealth target, ReactionContext context)
    {
        RemainingSeconds = remainingSeconds;
        Target = target;
        Context = context;
    }
}