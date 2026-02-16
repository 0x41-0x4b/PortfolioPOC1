using UnityEngine;

/// <summary>
/// Drives enemy movement toward the player using Rigidbody physics.
/// Reads <see cref="Enemy.MoveSpeed"/> so slow/freeze effects are respected automatically.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    #region Configuration

    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 0.5f;

    #endregion

    #region Private Fields

    private Transform m_PlayerTransform;
    private Rigidbody m_Rigidbody;
    private Enemy m_Enemy;

    #endregion

    #region Lifecycle

    private void Start()
    {
        ValidateComponents();
    }

    private void FixedUpdate()
    {
        if (m_PlayerTransform != null)
            ChasePlayer();
    }

    #endregion

    #region Chase Logic

    private void ChasePlayer()
    {
        Vector3 direction = (m_PlayerTransform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, m_PlayerTransform.position);

        if (distance > stoppingDistance)
        {
            float speed = m_Enemy != null ? m_Enemy.MoveSpeed : chaseSpeed;
            Vector3 targetVelocity = direction * speed;
            m_Rigidbody.linearVelocity = new Vector3(targetVelocity.x, m_Rigidbody.linearVelocity.y, targetVelocity.z);
            FaceDirection(direction);
        }
        else
        {
            m_Rigidbody.linearVelocity = new Vector3(0f, m_Rigidbody.linearVelocity.y, 0f);
        }
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, target, Time.fixedDeltaTime * 5f);
    }

    #endregion

    #region Validation

    private void ValidateComponents()
    {
        m_PlayerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (m_PlayerTransform == null)
        {
            Debug.LogError("EnemyAI: No GameObject tagged 'Player' found.", gameObject);
            enabled = false;
            return;
        }

        m_Rigidbody = GetComponent<Rigidbody>();
        if (m_Rigidbody == null)
        {
            Debug.LogError("EnemyAI: Rigidbody component missing.", gameObject);
            enabled = false;
            return;
        }

        m_Enemy = GetComponent<Enemy>();
        if (m_Enemy == null)
        {
            Debug.LogError("EnemyAI: Enemy component missing.", gameObject);
            enabled = false;
            return;
        }

        m_Rigidbody.isKinematic = false;
        m_Rigidbody.useGravity = true;
        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        if (GetComponent<Collider>() == null)
        {
            Debug.LogError("EnemyAI: No Collider component found.", gameObject);
            enabled = false;
        }
    }

    #endregion
}
