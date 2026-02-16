using UnityEngine;

/// <summary>
/// Fireball projectile — deals direct damage on hit and applies a burn-over-time effect.
/// </summary>
public class FireballProjectile : AbilityProjectile
{
    [Header("Fireball Effect")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float burnDamage = 5f;
    [SerializeField] private float burnDuration = 3f;
    [SerializeField] private float burnTickInterval = 0.5f;

    protected override void OnImpact(Collision collision)
    {
        Enemy enemy = collision.collider.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
            enemy.ApplyBurn(burnDamage, burnDuration, burnTickInterval);
        }

        Destroy(gameObject);
    }
}
