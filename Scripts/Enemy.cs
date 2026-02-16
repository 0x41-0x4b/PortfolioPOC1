using UnityEngine;
using System.Collections;

/// <summary>
/// Core enemy component — manages health, status effects (burn/slow/freeze),
/// melee attacks, visual feedback, and a looping squish animation.
/// </summary>
public class Enemy : MonoBehaviour
{
    #region Configuration

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float baseMoveSpeed = 3f;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;

    #endregion

    #region Runtime State

    private float m_CurrentHealth;
    private float m_LastAttackTime = -999f;
    private InteractionSystem m_PlayerRef;

    // Status-effect coroutine handles
    private Coroutine m_BurnCoroutine;
    private Coroutine m_FreezeCoroutine;
    private Coroutine m_SlowCoroutine;

    private float m_CurrentSlowAmount;
    private bool m_IsFrozen;

    // Visual
    private Renderer m_Renderer;
    private Color m_OriginalColor;

    #endregion

    #region Public Accessors

    /// <summary>
    /// Current move speed, factoring in slow/freeze effects.
    /// Read by <see cref="EnemyAI"/> to drive chase velocity.
    /// </summary>
    public float MoveSpeed { get; private set; }

    #endregion

    #region Lifecycle

    private void Awake()
    {
        m_CurrentHealth = maxHealth;
        MoveSpeed = baseMoveSpeed;

        m_Renderer = GetComponentInChildren<Renderer>();
        if (m_Renderer != null)
            m_OriginalColor = m_Renderer.material.color;

        m_PlayerRef = FindFirstObjectByType<InteractionSystem>();

        StartCoroutine(BouncySquishAnimation());
    }

    private void Update()
    {
        if (m_PlayerRef == null || m_IsFrozen)
            return;

        float distance = Vector3.Distance(transform.position, m_PlayerRef.transform.position);
        if (distance <= attackRange && Time.time >= m_LastAttackTime + attackCooldown)
            PerformAttack();
    }

    #endregion

    #region Public API — Damage & Status Effects

    /// <summary>Applies immediate damage and flashes the enemy red.</summary>
    public void ApplyDamage(float amount)
    {
        m_CurrentHealth -= amount;

        if (m_Renderer != null)
            StartCoroutine(FlashColor(Color.red, 0.15f));

        if (m_CurrentHealth <= 0f)
            Die();
    }

    /// <summary>Applies a damage-over-time burn effect.</summary>
    public void ApplyBurn(float tickDamage, float duration, float tickInterval)
    {
        if (m_BurnCoroutine != null)
            StopCoroutine(m_BurnCoroutine);

        m_BurnCoroutine = StartCoroutine(BurnRoutine(tickDamage, duration, tickInterval));
    }

    /// <summary>Reduces move speed by a percentage for a duration. Does not stack.</summary>
    public void ApplySlow(float slowAmount, float duration)
    {
        if (m_SlowCoroutine != null)
            return; // Already slowed — don't stack

        m_SlowCoroutine = StartCoroutine(SlowRoutine(slowAmount, duration));
    }

    /// <summary>Freezes the enemy in place for a duration, halting movement and animation.</summary>
    public void ApplyFreeze(float duration)
    {
        if (m_FreezeCoroutine != null)
            StopCoroutine(m_FreezeCoroutine);

        m_FreezeCoroutine = StartCoroutine(FreezeRoutine(duration));
    }

    #endregion

    #region Combat

    private void PerformAttack()
    {
        if (m_PlayerRef == null)
            return;

        m_LastAttackTime = Time.time;
        Vector3 knockbackDir = (m_PlayerRef.transform.position - transform.position).normalized;
        m_PlayerRef.TakeDamage(attackDamage, knockbackDir, 15f);
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    #endregion

    #region Status-Effect Coroutines

    private IEnumerator BurnRoutine(float tickDamage, float duration, float tickInterval)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            ApplyDamage(tickDamage);
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        m_BurnCoroutine = null;
    }

    private IEnumerator SlowRoutine(float slowAmount, float duration)
    {
        m_CurrentSlowAmount = slowAmount;
        MoveSpeed = baseMoveSpeed * (1f - slowAmount);

        if (m_Renderer != null)
            m_Renderer.material.color = Color.blue;

        yield return new WaitForSeconds(duration);

        MoveSpeed = baseMoveSpeed;
        m_CurrentSlowAmount = 0f;

        if (m_Renderer != null)
            m_Renderer.material.color = m_OriginalColor;

        m_SlowCoroutine = null;
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        m_IsFrozen = true;
        MoveSpeed = 0f;

        const float flashInterval = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (m_Renderer != null)
                StartCoroutine(FlashColor(Color.cyan, flashInterval));

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        m_IsFrozen = false;

        // Restore speed: respect any slow that was active before the freeze
        MoveSpeed = m_CurrentSlowAmount > 0f
            ? baseMoveSpeed * (1f - m_CurrentSlowAmount)
            : baseMoveSpeed;

        if (m_Renderer != null)
            m_Renderer.material.color = m_OriginalColor;

        m_FreezeCoroutine = null;
    }

    #endregion

    #region Visual Feedback

    private IEnumerator FlashColor(Color color, float duration)
    {
        if (m_Renderer == null)
            yield break;

        m_Renderer.material.color = color;
        yield return new WaitForSeconds(duration);
        m_Renderer.material.color = m_OriginalColor;
    }

    /// <summary>
    /// Continuous squish animation that scales Y between 1 and 0.5.
    /// Pauses automatically while the enemy is frozen.
    /// </summary>
    private IEnumerator BouncySquishAnimation()
    {
        Vector3 normalScale = Vector3.one;
        Vector3 squishScale = new Vector3(1f, 0.5f, 1f);
        const float phaseDuration = 0.3f;

        while (true)
        {
            if (m_IsFrozen)
            {
                yield return null;
                continue;
            }

            // Squish down
            yield return LerpScale(normalScale, squishScale, phaseDuration);

            // Expand back
            yield return LerpScale(squishScale, normalScale, phaseDuration);

            if (!m_IsFrozen)
                transform.localScale = normalScale;
        }
    }

    private IEnumerator LerpScale(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !m_IsFrozen)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }
    }

    #endregion
}

