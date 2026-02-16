using UnityEngine;

/// <summary>
/// ScriptableObject that defines a player ability's metadata and cooldown.
/// Create instances via Assets > Create > Abilities > AbilityDefinition.
/// </summary>
[CreateAssetMenu(fileName = "AbilityDefinition", menuName = "Abilities/AbilityDefinition")]
public class AbilityDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown in UI.")]
    [SerializeField] private string abilityName = "Ability";

    [Tooltip("Icon displayed in the ability bar.")]
    [SerializeField] private Sprite icon;

    [Header("Timing")]
    [Tooltip("Seconds between casts.")]
    [SerializeField] private float cooldown;

    /// <summary>Display name of this ability.</summary>
    public string AbilityName => abilityName;

    /// <summary>Icon sprite for the ability bar slot.</summary>
    public Sprite Icon => icon;

    /// <summary>Cooldown duration in seconds.</summary>
    public float Cooldown => cooldown;
}
