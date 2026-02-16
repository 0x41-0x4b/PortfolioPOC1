using UnityEngine;

/// <summary>
/// Contract for objects the player can interact with via raycast.
/// Implement on any GameObject that should respond to the player's interact input.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Invoked when the player presses the interact key while aiming at this object.
    /// </summary>
    void Interact();

    /// <summary>
    /// Returns this object's Transform, used for UI prompt positioning.
    /// </summary>
    Transform GetInteractableTransform();
}
