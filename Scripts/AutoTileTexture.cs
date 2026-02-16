using UnityEngine;

/// <summary>
/// Automatically adjusts material texture tiling to match the object’s world scale,
/// preventing stretching on non-uniform objects.
/// Attach to any GameObject with a Renderer.
/// </summary>
[ExecuteInEditMode]
public class AutoTileTexture : MonoBehaviour
{
    #region Configuration

    [Tooltip("Base scale that the texture was authored for (default 1×1×1).")]
    [SerializeField] private Vector3 baseScale = Vector3.one;

    [Tooltip("Multiplier applied on top of the calculated tiling.")]
    [SerializeField] private float tilingMultiplier = 1f;

    [Tooltip("Re-evaluate tiling every frame (use for runtime-scaled objects).")]
    [SerializeField] private bool updateContinuously;

    #endregion

    #region Private Fields

    private Renderer m_Renderer;
    private MaterialPropertyBlock m_PropertyBlock;
    private Vector3 m_LastScale;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
        m_PropertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        ApplyTiling();
    }

    private void Update()
    {
        if (updateContinuously && transform.lossyScale != m_LastScale)
            ApplyTiling();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            ApplyTiling();
#endif
    }

    private void OnValidate()
    {
        ApplyTiling();
    }

    #endregion

    #region Tiling

    /// <summary>
    /// Calculates and applies texture tiling based on world scale.
    /// </summary>
    public void ApplyTiling()
    {
        if (m_Renderer == null)
            m_Renderer = GetComponent<Renderer>();

        if (m_Renderer == null)
            return;

        if (m_PropertyBlock == null)
            m_PropertyBlock = new MaterialPropertyBlock();

        Vector3 scale = transform.lossyScale;
        m_LastScale = scale;

        Material[] materials = Application.isPlaying ? m_Renderer.materials : m_Renderer.sharedMaterials;

        foreach (Material mat in materials)
        {
            if (mat == null)
                continue;

            Vector2 tiling = new Vector2(
                (scale.x / baseScale.x) * tilingMultiplier,
                (scale.y / baseScale.y) * tilingMultiplier);

            mat.mainTextureScale = tiling;
        }
    }

    #endregion
}
