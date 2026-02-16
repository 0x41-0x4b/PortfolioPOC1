using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person CharacterController-based movement with momentum, gravity, jumping, and knockback.
/// Receives input callbacks from the Unity PlayerInput component.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    #region Configuration

    [Header("Horizontal Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 15f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedYVelocity = -2f;

    #endregion

    #region Constants

    private const float InputThreshold = 0.01f;

    #endregion

    #region Private Fields

    private CharacterController m_CharacterController;
    private Vector2 m_MoveInput;
    private Vector3 m_Velocity;
    private Vector3 m_HorizontalVelocity;
    private Vector3 m_KnockbackVelocity;
    private bool m_JumpRequested;

    #endregion

    #region Lifecycle

    private void Start()
    {
        ValidateComponents();
    }

    private void Update()
    {
        ApplyMovement(m_MoveInput);
    }

    #endregion

    #region Input Callbacks

    /// <summary>Called by PlayerInput when movement input changes.</summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        m_MoveInput = context.ReadValue<Vector2>();
    }

    /// <summary>Called by PlayerInput when jump input is triggered. Only queues a jump while grounded.</summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && m_CharacterController.isGrounded)
            m_JumpRequested = true;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Applies an impulse knockback through the CharacterController.
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force)
    {
        Vector3 knockback = direction.normalized * force;
        m_KnockbackVelocity = new Vector3(knockback.x, 0f, knockback.z);
    }

    #endregion

    #region Movement Logic

    private void ApplyMovement(Vector2 direction)
    {
        Vector3 desired = CalculateDesiredVelocity(direction);
        ApplyMomentum(desired);
        HandleVerticalMovement();

        m_KnockbackVelocity = Vector3.Lerp(m_KnockbackVelocity, Vector3.zero, 5f * Time.deltaTime);

        Vector3 finalVelocity = m_HorizontalVelocity + m_KnockbackVelocity + Vector3.up * m_Velocity.y;
        m_CharacterController.Move(finalVelocity * Time.deltaTime);
    }

    private Vector3 CalculateDesiredVelocity(Vector2 direction)
    {
        if (direction.sqrMagnitude < InputThreshold)
            return Vector3.zero;

        return GetWorldDirection(direction) * moveSpeed;
    }

    private void ApplyMomentum(Vector3 desired)
    {
        float factor = desired.sqrMagnitude > 0f ? acceleration : deceleration;
        m_HorizontalVelocity = Vector3.Lerp(m_HorizontalVelocity, desired, factor * Time.deltaTime);
    }

    private Vector3 GetWorldDirection(Vector2 input)
    {
        Quaternion yaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        return yaw * new Vector3(input.x, 0f, input.y);
    }

    #endregion

    #region Vertical Movement

    private void HandleVerticalMovement()
    {
        if (m_JumpRequested)
        {
            m_Velocity.y = jumpForce;
            m_JumpRequested = false;
        }
        else if (m_CharacterController.isGrounded)
        {
            m_Velocity.y = groundedYVelocity;
        }
        else
        {
            m_Velocity.y += gravity * Time.deltaTime;
        }
    }

    #endregion

    #region Validation

    private void ValidateComponents()
    {
        m_CharacterController = GetComponent<CharacterController>();

        if (m_CharacterController == null)
        {
            Debug.LogError("PlayerMovement: CharacterController component required.", gameObject);
            enabled = false;
        }
    }

    #endregion
}

