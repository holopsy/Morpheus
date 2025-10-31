using System;

public static class CheckpointEvents
{
    /// Fired right after the player is respawned at a checkpoint.
    public static event Action OnPlayerRespawned;

    public static void FirePlayerRespawned() => OnPlayerRespawned?.Invoke();
}