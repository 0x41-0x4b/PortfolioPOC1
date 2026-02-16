using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Central player system — handles interaction raycasting, ability casting,
/// HUD creation (crosshair, ability bar, health bar), health, and death/respawn.
/// Attach to the player capsule alongside PlayerInput and PlayerMovement.
/// </summary>
public class InteractionSystem : MonoBehaviour
{
    #region Configuration

    [Header("Interaction")]
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private Canvas interactionCanvas;
    [SerializeField] private TextMeshProUGUI interactionPrompt;
    [SerializeField] private float crosshairSize = 20f;
    [SerializeField] private float promptOffsetY = -40f;

    [Header("Abilities")]
    [SerializeField] private AbilityDefinition[] abilities = new AbilityDefinition[3];
    [SerializeField] private GameObject abilityProjectilePrefab;

    [Header("Player Health")]
    [SerializeField] private float maxHealth = 100f;

    #endregion

    #region Private Fields — HUD

    private Camera m_Camera;
    private PlayerInput m_PlayerInput;

    // Interaction
    private IInteractable m_CurrentInteractable;
    private bool m_InteractInputPressed;
    private bool m_PromptVisible;
    private CanvasGroup m_PromptCanvasGroup;
    private RectTransform m_PromptRect;
    private Image m_Crosshair;
    private RectTransform m_CrosshairRect;

    // Abilities
    private int m_SelectedAbilityIndex;
    private RectTransform m_AbilityBarRect;
    private Image[] m_AbilityIcons = new Image[3];
    private Image m_SelectionHighlight;
    private float[] m_AbilityLastCastTimes;

    // Health
    private float m_CurrentHealth;
    private Image m_HealthBarFill;
    private Image m_HealthBarBackground;
    private Coroutine m_HealthFlashCoroutine;

    #endregion

    #region Lifecycle

    private void Start()
    {
        m_CurrentHealth = maxHealth;
        ValidateComponents();
        WarmUpPhysics();
        InitializeCooldownTracking();
    }

    private void Update()
    {
        HandleAbilityInput();
        DetectInteractable();

        if (m_InteractInputPressed)
        {
            InteractWithTarget();
            m_InteractInputPressed = false;
        }
    }

    #endregion

    #region Interaction Detection

    private void DetectInteractable()
    {
        Ray ray = m_Camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        IInteractable hitObject = null;

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
            hitObject = hit.collider.GetComponent<IInteractable>();

        if (hitObject != m_CurrentInteractable)
        {
            m_CurrentInteractable = hitObject;
            UpdatePromptVisibility();
        }
    }

    private void UpdatePromptVisibility()
    {
        bool shouldBeVisible = m_CurrentInteractable != null;
        if (shouldBeVisible == m_PromptVisible)
            return;

        m_PromptCanvasGroup.alpha = shouldBeVisible ? 1f : 0f;
        m_PromptVisible = shouldBeVisible;
    }

    private void InteractWithTarget()
    {
        m_CurrentInteractable?.Interact();
    }

    #endregion

    #region Input Callbacks

    /// <summary>Called by PlayerInput when the interact key is pressed.</summary>
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
            m_InteractInputPressed = true;
    }

    #endregion

    #region Ability Input & UI

    private void InitializeCooldownTracking()
    {
        if (abilities == null)
            return;

        m_AbilityLastCastTimes = new float[abilities.Length];
        for (int i = 0; i < m_AbilityLastCastTimes.Length; i++)
            m_AbilityLastCastTimes[i] = -999f;
    }

    private void HandleAbilityInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) SelectAbility(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectAbility(1);
            if (keyboard.digit3Key.wasPressedThisFrame) SelectAbility(2);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scrollY = mouse.scroll.ReadValue().y;
            if (scrollY > 0.01f) SelectPreviousAbility();
            else if (scrollY < -0.01f) SelectNextAbility();

            if (mouse.leftButton.wasPressedThisFrame)
                TryCastSelectedAbility();
        }
    }

    private void SelectAbility(int index)
    {
        if (abilities == null || abilities.Length == 0) return;
        m_SelectedAbilityIndex = Mathf.Clamp(index, 0, abilities.Length - 1);
        UpdateAbilityUI();
    }

    private void SelectNextAbility()
    {
        if (abilities == null || abilities.Length == 0) return;
        m_SelectedAbilityIndex = (m_SelectedAbilityIndex + 1) % abilities.Length;
        UpdateAbilityUI();
    }

    private void SelectPreviousAbility()
    {
        if (abilities == null || abilities.Length == 0) return;
        m_SelectedAbilityIndex = (m_SelectedAbilityIndex - 1 + abilities.Length) % abilities.Length;
        UpdateAbilityUI();
    }

    private void UpdateAbilityUI()
    {
        for (int i = 0; i < m_AbilityIcons.Length; i++)
        {
            if (m_AbilityIcons[i] == null)
                continue;

            bool hasIcon = abilities != null
                && i < abilities.Length
                && abilities[i] != null
                && abilities[i].Icon != null;

            if (hasIcon)
            {
                m_AbilityIcons[i].sprite = abilities[i].Icon;
                m_AbilityIcons[i].color = Color.white;
                m_AbilityIcons[i].preserveAspect = true;
            }
            else
            {
                m_AbilityIcons[i].sprite = null;
                m_AbilityIcons[i].color = new Color(1f, 1f, 1f, 0.4f);
            }
        }

        if (m_SelectionHighlight != null
            && m_AbilityIcons.Length > m_SelectedAbilityIndex
            && m_AbilityIcons[m_SelectedAbilityIndex] != null)
        {
            m_SelectionHighlight.rectTransform.anchoredPosition =
                m_AbilityIcons[m_SelectedAbilityIndex].rectTransform.anchoredPosition;
        }
    }

    private void TryCastSelectedAbility()
    {
        if (abilities == null || m_SelectedAbilityIndex < 0 || m_SelectedAbilityIndex >= abilities.Length)
            return;

        AbilityDefinition definition = abilities[m_SelectedAbilityIndex];
        if (definition == null)
            return;

        // Cooldown check
        if (m_AbilityLastCastTimes == null)
            m_AbilityLastCastTimes = new float[abilities.Length];

        float lastCast = m_AbilityLastCastTimes[m_SelectedAbilityIndex];
        if (Time.time < lastCast + definition.Cooldown)
            return;

        m_AbilityLastCastTimes[m_SelectedAbilityIndex] = Time.time;
        SpawnProjectile(definition);
    }

    private void SpawnProjectile(AbilityDefinition definition)
    {
        if (abilityProjectilePrefab == null)
        {
            Debug.LogError("InteractionSystem: Ability projectile prefab not assigned.", gameObject);
            return;
        }

        Vector3 spawnPos = m_Camera.transform.position + m_Camera.transform.forward * 1.5f;
        Quaternion rotation = Quaternion.LookRotation(m_Camera.transform.forward);
        GameObject projectileGO = Instantiate(abilityProjectilePrefab, spawnPos, rotation);

        AbilityProjectile projectile = projectileGO.GetComponent<AbilityProjectile>();
        if (projectile == null)
        {
            Debug.LogError("InteractionSystem: Prefab missing AbilityProjectile component.", gameObject);
            Destroy(projectileGO);
            return;
        }

        projectile.InitializeComponents();
        projectile.SetAbilityColor(m_SelectedAbilityIndex);
        AttachAbilityBehaviour(projectileGO, m_SelectedAbilityIndex);
        projectile.Launch(m_Camera.transform.forward);
    }

    /// <summary>
    /// Attaches the correct projectile sub-type component at runtime based on the selected ability slot.
    /// </summary>
    private void AttachAbilityBehaviour(GameObject projectile, int abilityIndex)
    {
        // Clean up any pre-existing ability components
        RemoveComponent<FireballProjectile>(projectile);
        RemoveComponent<FrostballProjectile>(projectile);
        RemoveComponent<TeleportballProjectile>(projectile);

        switch (abilityIndex)
        {
            case 0: projectile.AddComponent<FireballProjectile>(); break;
            case 1: projectile.AddComponent<FrostballProjectile>(); break;
            case 2: projectile.AddComponent<TeleportballProjectile>(); break;
        }
    }

    private static void RemoveComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component != null)
            Destroy(component);
    }

    #endregion

    #region Teleport

    /// <summary>
    /// Teleports the player to a world position after a short delay.
    /// Used by <see cref="TeleportballProjectile"/>.
    /// </summary>
    public void TeleportToPosition(Vector3 position, float delay)
    {
        StartCoroutine(TeleportRoutine(position, delay));
    }

    private IEnumerator TeleportRoutine(Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = position;
        if (cc != null) cc.enabled = true;
    }

    #endregion

    #region Player Health & Damage

    /// <summary>
    /// Inflicts damage and knockback on the player. Called by enemies.
    /// </summary>
    public void TakeDamage(float damage, Vector3 knockbackDirection, float knockbackForce)
    {
        m_CurrentHealth = Mathf.Max(0f, m_CurrentHealth - damage);

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.ApplyKnockback(knockbackDirection, knockbackForce);

        if (m_HealthFlashCoroutine != null)
            StopCoroutine(m_HealthFlashCoroutine);
        m_HealthFlashCoroutine = StartCoroutine(HealthBarFlashRoutine());

        if (m_CurrentHealth <= 0f)
            Die();
    }

    private IEnumerator HealthBarFlashRoutine()
    {
        if (m_HealthBarFill == null) yield break;

        const float flashDuration = 0.25f;
        const float flashInterval = 0.05f;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += flashInterval;
            m_HealthBarFill.color = (elapsed % 0.1f) < 0.05f ? Color.red : Color.yellow;
            yield return new WaitForSeconds(flashInterval);
        }

        m_HealthBarFill.color = Color.green;
        UpdateHealthBar();
        m_HealthFlashCoroutine = null;
    }

    private void UpdateHealthBar()
    {
        if (m_HealthBarFill == null)
            return;

        float percent = Mathf.Clamp01(m_CurrentHealth / maxHealth);
        m_HealthBarFill.rectTransform.anchorMax = new Vector2(percent, 1f);
        m_HealthBarFill.color = Color.green;
    }

    private void Die()
    {
        // Destroy all active enemies
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
            Destroy(enemy.gameObject);

        // Respawn at origin
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = new Vector3(0f, 1f, 0f);
        if (cc != null) cc.enabled = true;

        // Restore health
        m_CurrentHealth = maxHealth;

        if (m_HealthBarFill != null)
        {
            m_HealthBarFill.color = Color.green;
            m_HealthBarFill.rectTransform.anchorMax = Vector2.one;
        }
    }

    #endregion

    #region Validation & HUD Setup

    private void ValidateComponents()
    {
        m_Camera = Camera.main;
        if (m_Camera == null)
        {
            Debug.LogError("InteractionSystem: Camera tagged 'MainCamera' required.", gameObject);
            enabled = false;
            return;
        }

        m_PlayerInput = GetComponent<PlayerInput>();
        if (m_PlayerInput == null)
        {
            Debug.LogError("InteractionSystem: PlayerInput component required.", gameObject);
            enabled = false;
            return;
        }

        if (interactionCanvas == null)
        {
            Debug.LogError("InteractionSystem: Canvas not assigned.", gameObject);
            enabled = false;
            return;
        }

        if (interactionPrompt == null)
        {
            Debug.LogError("InteractionSystem: Interaction prompt text not assigned.", gameObject);
            enabled = false;
            return;
        }

        // Prompt visibility via CanvasGroup
        m_PromptCanvasGroup = interactionPrompt.GetComponent<CanvasGroup>();
        if (m_PromptCanvasGroup == null)
            m_PromptCanvasGroup = interactionPrompt.gameObject.AddComponent<CanvasGroup>();

        m_PromptRect = interactionPrompt.GetComponent<RectTransform>();

        CreateOrFindCrosshair();
        CreateOrFindAbilityBar();
        CreateOrFindHealthBar();
    }

    private void WarmUpPhysics()
    {
        Physics.Raycast(Vector3.zero, Vector3.down, 0.1f);

        if (m_PromptCanvasGroup != null)
            m_PromptCanvasGroup.alpha = 0f;

        if (m_Crosshair != null)
            m_Crosshair.enabled = true;
    }

    #endregion

    #region HUD Construction

    private void CreateOrFindCrosshair()
    {
        Transform existing = interactionCanvas.transform.Find("Crosshair");
        if (existing != null)
        {
            m_Crosshair = existing.GetComponent<Image>();
            m_CrosshairRect = existing.GetComponent<RectTransform>();
            PositionPromptBelowCrosshair();
            return;
        }

        GameObject go = new GameObject("Crosshair");
        go.transform.SetParent(interactionCanvas.transform, false);

        m_CrosshairRect = go.AddComponent<RectTransform>();
        m_Crosshair = go.AddComponent<Image>();

        m_CrosshairRect.anchoredPosition = Vector2.zero;
        m_CrosshairRect.sizeDelta = new Vector2(crosshairSize, crosshairSize);
        m_Crosshair.color = Color.white;

        PositionPromptBelowCrosshair();
    }

    private void CreateOrFindAbilityBar()
    {
        if (interactionCanvas == null) return;

        Transform existing = interactionCanvas.transform.Find("AbilityBar");
        if (existing != null)
        {
            m_AbilityBarRect = existing as RectTransform;
            for (int i = 0; i < 3; i++)
            {
                Transform slot = existing.Find($"AbilitySlot{i}");
                if (slot != null) m_AbilityIcons[i] = slot.GetComponent<Image>();
            }
            Transform highlight = existing.Find("SelectionHighlight");
            if (highlight != null) m_SelectionHighlight = highlight.GetComponent<Image>();
            UpdateAbilityUI();
            return;
        }

        const float slotSize = 90f;
        const float slotGap = 10f;

        GameObject barGO = new GameObject("AbilityBar");
        barGO.transform.SetParent(interactionCanvas.transform, false);

        m_AbilityBarRect = barGO.AddComponent<RectTransform>();
        m_AbilityBarRect.anchorMin = new Vector2(0.5f, 0f);
        m_AbilityBarRect.anchorMax = new Vector2(0.5f, 0f);
        m_AbilityBarRect.pivot = new Vector2(0.5f, 0f);
        m_AbilityBarRect.sizeDelta = new Vector2(360f, slotSize);
        m_AbilityBarRect.anchoredPosition = new Vector2(0f, 50f);

        for (int i = 0; i < 3; i++)
        {
            GameObject slot = new GameObject($"AbilitySlot{i}");
            slot.transform.SetParent(barGO.transform, false);

            RectTransform rt = slot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(slotSize, slotSize);
            rt.anchoredPosition = new Vector2((i - 1) * (slotSize + slotGap), 0f);

            Image icon = slot.AddComponent<Image>();
            icon.color = new Color(1f, 1f, 1f, 0.8f);
            m_AbilityIcons[i] = icon;
        }

        // Selection highlight sits behind the selected slot
        GameObject highlightGO = new GameObject("SelectionHighlight");
        highlightGO.transform.SetParent(barGO.transform, false);

        RectTransform hrt = highlightGO.AddComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(slotSize + 8f, slotSize + 8f);
        hrt.anchoredPosition = Vector2.zero;

        m_SelectionHighlight = highlightGO.AddComponent<Image>();
        m_SelectionHighlight.color = new Color(1f, 1f, 0.4f, 0.35f);

        UpdateAbilityUI();
    }

    private void CreateOrFindHealthBar()
    {
        if (interactionCanvas == null) return;

        Transform existing = interactionCanvas.transform.Find("HealthBar");
        if (existing != null)
        {
            m_HealthBarBackground = existing.GetComponent<Image>();
            Transform fill = existing.Find("HealthFill");
            if (fill != null) m_HealthBarFill = fill.GetComponent<Image>();
            UpdateHealthBar();
            return;
        }

        // Background
        GameObject barGO = new GameObject("HealthBar");
        barGO.transform.SetParent(interactionCanvas.transform, false);

        RectTransform barRT = barGO.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0.5f, 1f);
        barRT.anchorMax = new Vector2(0.5f, 1f);
        barRT.pivot = new Vector2(0.5f, 1f);
        barRT.sizeDelta = new Vector2(300f, 30f);
        barRT.anchoredPosition = new Vector2(0f, -20f);

        m_HealthBarBackground = barGO.AddComponent<Image>();
        m_HealthBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Fill
        GameObject fillGO = new GameObject("HealthFill");
        fillGO.transform.SetParent(barGO.transform, false);

        RectTransform fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        fillRT.pivot = new Vector2(0f, 0.5f);

        m_HealthBarFill = fillGO.AddComponent<Image>();
        m_HealthBarFill.color = Color.green;

        UpdateHealthBar();
    }

    private void PositionPromptBelowCrosshair()
    {
        if (m_PromptRect != null)
            m_PromptRect.anchoredPosition = new Vector2(0f, promptOffsetY);
    }

    #endregion
}

