using UnityEngine;

/// <summary>
/// Frostball projectile — applies an area-of-effect slow to nearby enemies on impact,
/// with a random chance to fully freeze each target.
/// </summary>
public class FrostballProjectile : AbilityProjectile
{
    [Header("Frostball Effect")]
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float slowAmount = 0.5f;
    [SerializeField] private float freezeChance = 0.2f;
    [SerializeField] private float freezeDuration = 1.5f;
    [SerializeField] private float areaRadius = 3f;

    protected override void OnImpact(Collision collision)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, areaRadius);

        foreach (Collider col in hitColliders)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy == null)
                continue;

            enemy.ApplySlow(slowAmount, slowDuration);

            if (Random.value < freezeChance)
                enemy.ApplyFreeze(freezeDuration);
        }

        Destroy(gameObject);
    }
}
