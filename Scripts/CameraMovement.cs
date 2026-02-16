using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person camera controller.
/// Yaw rotates the parent body (horizontal), pitch rotates only the camera (vertical).
/// Must be a child of the player capsule.
/// </summary>
public class CameraMovement : MonoBehaviour
{
    #region Configuration

    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private float maxPitchAngle = 89f;
    [SerializeField] private float minPitchAngle = -89f;

    #endregion

    #region Constants

    private const float InputThreshold = 0.01f;

    #endregion

    #region Private Fields

    private float m_Pitch;
    private Vector2 m_LookInput;
    private Transform m_ParentTransform;

    #endregion

    #region Lifecycle

    private void Start()
    {
        ValidateComponents();
    }

    private void Update()
    {
        ApplyLook(m_LookInput);
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Called by the PlayerInput component when the look input changes.
    /// </summary>
    public void OnLook(InputAction.CallbackContext context)
    {
        m_LookInput = context.ReadValue<Vector2>();
    }

    #endregion

    #region Look Logic

    private void ApplyLook(Vector2 lookInput)
    {
        if (lookInput.sqrMagnitude < InputThreshold)
            return;

        float scaledSpeed = rotateSpeed * Time.deltaTime;

        ApplyYaw(lookInput.x, scaledSpeed);
        ApplyPitch(lookInput.y, scaledSpeed);
    }

    private void ApplyYaw(float yawInput, float scaledSpeed)
    {
        m_ParentTransform.Rotate(0f, yawInput * scaledSpeed, 0f, Space.Self);
    }

    private void ApplyPitch(float pitchInput, float scaledSpeed)
    {
        m_Pitch = Mathf.Clamp(m_Pitch - pitchInput * scaledSpeed, minPitchAngle, maxPitchAngle);
        transform.localEulerAngles = new Vector3(m_Pitch, 0f, 0f);
    }

    #endregion

    #region Validation

    private void ValidateComponents()
    {
        m_ParentTransform = transform.parent;

        if (m_ParentTransform == null)
        {
            Debug.LogError(
                "CameraMovement requires a parent Transform (player body). " +
                "This camera must be a child of the player capsule.", gameObject);
            enabled = false;
        }
    }

    #endregion
}
