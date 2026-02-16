using UnityEngine;
using System.Collections;

/// <summary>
/// Interactable button that flashes green and spawns enemies around the player.
/// </summary>
public class ButtonInteraction : MonoBehaviour, IInteractable
{
    #region Configuration

    [Header("Spawning")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int enemiesToSpawn = 3;
    [SerializeField] private float minSpawnDistance = 15f;
    [SerializeField] private float maxSpawnDistance = 30f;

    [Header("Visual Feedback")]
    [SerializeField] private float colorChangeDuration = 0.5f;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color normalColor = Color.red;

    #endregion

    #region Private Fields

    private Material m_ButtonMaterial;
    private Transform m_PlayerTransform;
    private Coroutine m_ColorChangeCoroutine;

    #endregion

    #region Lifecycle

    private void Start()
    {
        ValidateComponents();
        SetButtonColor(normalColor);
    }

    #endregion

    #region IInteractable

    /// <inheritdoc/>
    public void Interact()
    {
        if (m_ColorChangeCoroutine != null)
            StopCoroutine(m_ColorChangeCoroutine);

        m_ColorChangeCoroutine = StartCoroutine(HandleInteraction());
    }

    /// <inheritdoc/>
    public Transform GetInteractableTransform()
    {
        return transform;
    }

    #endregion

    #region Interaction Logic

    private IEnumerator HandleInteraction()
    {
        SetButtonColor(activeColor);
        yield return new WaitForSeconds(colorChangeDuration);
        SetButtonColor(normalColor);

        SpawnEnemies();
    }

    private void SetButtonColor(Color color)
    {
        if (m_ButtonMaterial != null)
            m_ButtonMaterial.color = color;
    }

    private void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("ButtonInteraction: Enemy prefab not assigned.", gameObject);
            return;
        }

        if (m_PlayerTransform == null)
        {
            Debug.LogError("ButtonInteraction: Player transform not found.", gameObject);
            return;
        }

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

            Vector3 spawnPosition = m_PlayerTransform.position + direction * distance;
            spawnPosition.y = m_PlayerTransform.position.y;

            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
    }

    #endregion

    #region Validation

    private void ValidateComponents()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("ButtonInteraction: Renderer component required.", gameObject);
            enabled = false;
            return;
        }

        m_ButtonMaterial = renderer.material;

        m_PlayerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (m_PlayerTransform == null)
        {
            Debug.LogError("ButtonInteraction: No GameObject tagged 'Player' found.", gameObject);
        }
    }

    #endregion
}

