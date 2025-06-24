using System.Collections;
using UnityEngine;

/// <author>Daniel G</author>
/// <summary>
/// Handles launching a projectile in a parabolic arc from a start position to a target position.
/// </summary>
public class ProjectileLauncher : MonoBehaviour
{
    [Header("Projectile Settings")] [Tooltip("Prefab for the projectile to launch.")] [SerializeField]
    private GameObject projectilePrefab;

    [Tooltip("Target transform the projectile will aim for.")] [SerializeField]
    private Transform target;

    [Tooltip("Time in seconds for the projectile to reach the target.")] [SerializeField]
    private float timeToTarget = 2f;

    [Tooltip("Maximum height of the projectile's arc above the higher of start or end point.")] [SerializeField]
    private float maxHeight = 1f;

    [Header("Explosion Settings")] [Tooltip("Radius of the explosion effect at the target position.")] [SerializeField]
    private float explosionRadius = 1f;

    [Tooltip("Force applied by the explosion to nearby rigidbodies.")] [SerializeField]
    private float explosionForce = 1000f;

    [Header("Auto Launch Settings")] [Tooltip("Delay in seconds between automatic launches.")] [SerializeField]
    private float autoLaunchDelay = 2f;

    [Tooltip("Enable to launch projectiles automatically at intervals.")] [SerializeField]
    private bool autoLaunch = true;

    [SerializeField] private AudioSource launcherSound;
    [SerializeField] private AudioSource explosionSound;

    /// <summary>
    /// Starts automatic launching if enabled.
    /// </summary>
    private void Start()
    {
        if (autoLaunch)
        {
            InvokeRepeating("LaunchProjectile", autoLaunchDelay, autoLaunchDelay);
        }
    }

    /// <summary>
    /// Launches a projectile from this object's position to the target.
    /// </summary>
    public void LaunchProjectile()
    {
        LaunchProjectile(transform.position, target.position);
    }

    /// <summary>
    /// Launches a projectile from a specified start position to a target position.
    /// </summary>
    /// <param name="startPosition">The starting position of the projectile.</param>
    /// <param name="targetPosition">The target position for the projectile.</param>
    public void LaunchProjectile(Vector3 startPosition, Vector3 targetPosition)
    {
        launcherSound.Play();
        GameObject projectile = Instantiate(projectilePrefab, startPosition, Quaternion.identity);
        projectile.transform.rotation = Quaternion.LookRotation(targetPosition - projectile.transform.position);
        StartCoroutine(SimulateProjectile(projectile, startPosition, targetPosition, timeToTarget, maxHeight));
    }

    /// <summary>
    /// Animates the projectile along a quadratic Bezier curve from start to end, simulating a parabolic arc.
    /// </summary>
    /// <param name="projectile">The projectile GameObject to move.</param>
    /// <param name="start">The starting position of the projectile.</param>
    /// <param name="end">The target position for the projectile.</param>
    /// <param name="duration">Time in seconds for the projectile to reach the target.</param>
    /// <param name="maxHeight">Maximum height above the higher of start or end point for the arc.</param>
    /// <returns>IEnumerator for coroutine animation.</returns>
    private IEnumerator SimulateProjectile(GameObject projectile, Vector3 start, Vector3 end,
        float duration, float maxHeight)
    {
        float timePassed = 0f;
        Vector3 midPoint = (start + end) / 2f;
        midPoint.y = Mathf.Max(start.y, end.y) + maxHeight;

        while (timePassed < duration)
        {
            float t = timePassed / duration;

            Vector3 position = Mathf.Pow(1 - t, 2) * start +
                               2 * (1 - t) * t * midPoint +
                               Mathf.Pow(t, 2) * end;

            projectile.transform.position = position;

            timePassed += Time.deltaTime;
            yield return null;
        }

        projectile.transform.position = end;
        Explode(projectile.transform.position);
        Destroy(projectile);
    }

    private void Explode(Vector3 explodePosition)
    {
        Collider[] colliders = Physics.OverlapSphere(explodePosition, explosionRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.attachedRigidbody)
            {
                collider.attachedRigidbody.AddExplosionForce(explosionForce, explodePosition, explosionRadius);
            }
        }
        explosionSound.Play();
    }

    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawWireSphere(target.position, explosionRadius);
        }
    }
}