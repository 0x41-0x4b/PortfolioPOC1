using UnityEngine;

/// <summary>
/// Controls cursor visibility and lock state.
/// Attach to any persistent GameObject in the scene.
/// </summary>
public class MouseLock : MonoBehaviour
{
    [SerializeField] private bool showCursor;
    [SerializeField] private CursorLockMode lockMode = CursorLockMode.Confined;

    private void Start()
    {
        ApplyCursorSettings();
    }

    private void OnValidate()
    {
        ApplyCursorSettings();
    }

    private void ApplyCursorSettings()
    {
        Cursor.visible = showCursor;
        Cursor.lockState = lockMode;
    }
}
