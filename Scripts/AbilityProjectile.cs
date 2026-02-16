using UnityEngine;

/// <summary>
/// Base class for all ability projectiles.
/// Handles movement, lifetime, color assignment, and collision routing.
/// Subclasses override <see cref="OnImpact"/> for ability-specific effects.
/// </summary>
public class AbilityProjectile : MonoBehaviour
{
    #region Configuration

    [Header("Movement")]
    [SerializeField] protected float speed = 20f;
    [SerializeField] protected float lifetime = 5f;

    [Header("Rendering")]
    [SerializeField] protected Renderer m_Renderer;

    #endregion

    #region Runtime State

    protected Rigidbody m_Rigidbody;
    protected float m_SpawnTime;
    protected bool m_HasLaunched;

    #endregion

    #region Lifecycle

    protected virtual void Start()
    {
        InitializeComponents();
    }

    protected virtual void Update()
    {
        if (Time.time - m_SpawnTime > lifetime)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Resolves internal component references. Called automatically in <see cref="Start"/>
    /// or manually for immediate use after instantiation.
    /// </summary>
    public virtual void InitializeComponents()
    {
        if (m_Rigidbody == null)
            m_Rigidbody = GetComponent<Rigidbody>();

        if (m_Renderer == null)
            m_Renderer = GetComponent<Renderer>();

        m_SpawnTime = Time.time;
    }

    /// <summary>
    /// Propels the projectile in the given direction at configured speed.
    /// </summary>
    public virtual void Launch(Vector3 direction)
    {
        if (m_Rigidbody == null)
        {
            Debug.LogError("Launch failed — Rigidbody not found.", gameObject);
            return;
        }

        m_Rigidbody.linearVelocity = direction.normalized * speed;
        m_HasLaunched = true;
    }

    /// <summary>
    /// Tints the projectile's material to match the ability type.
    /// </summary>
    public virtual void SetAbilityColor(int abilityIndex)
    {
        if (m_Renderer == null)
            m_Renderer = GetComponent<Renderer>();

        if (m_Renderer == null)
        {
            Debug.LogError($"{name}: Renderer not found for color assignment.", gameObject);
            return;
        }

        Color color = GetColorForAbility(abilityIndex);
        Material mat = m_Renderer.material;
        mat.SetColor("_Color", color);
        mat.color = color;
    }

    #endregion

    #region Collision

    /// <summary>
    /// Routes collisions to <see cref="OnImpact"/>, ignoring the player.
    /// </summary>
    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            return;

        OnImpact(collision);
    }

    /// <summary>
    /// Override in subclasses to implement ability-specific impact behaviour.
    /// Base implementation destroys the projectile.
    /// </summary>
    protected virtual void OnImpact(Collision collision)
    {
        Destroy(gameObject);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Maps an ability-slot index to a projectile tint colour.
    /// </summary>
    protected virtual Color GetColorForAbility(int abilityIndex)
    {
        return abilityIndex switch
        {
            0 => new Color(1f, 0f, 0f, 1f),       // Red — Fireball
            1 => new Color(0.3f, 0.8f, 1f, 1f),   // Cyan — Frostball
            2 => new Color(0.8f, 0.5f, 1f, 1f),   // Purple — Teleportball
            _ => Color.white
        };
    }

    #endregion
}
