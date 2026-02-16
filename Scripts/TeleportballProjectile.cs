using UnityEngine;

/// <summary>
/// Teleportball projectile — teleports the player to the impact point.
/// </summary>
public class TeleportballProjectile : AbilityProjectile
{
    [Header("Teleport Effect")]
    [SerializeField] private float teleportDelay = 0.05f;

    protected override void OnImpact(Collision collision)
    {
        InteractionSystem player = FindFirstObjectByType<InteractionSystem>();
        if (player != null)
            player.TeleportToPosition(transform.position, teleportDelay);

        Destroy(gameObject);
    }
}
