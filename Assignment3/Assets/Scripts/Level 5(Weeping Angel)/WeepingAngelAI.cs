//using UnityEngine;
//using UnityEngine.AI;

//[RequireComponent(typeof(NavMeshAgent))]
//public class WeepingAngelAI : MonoBehaviour
//{
//    [SerializeField] private Renderer angelRenderer;     // the mesh used for the seen check
//    [SerializeField] private LayerMask obstructionMask;  // Walls layer ONLY
//    [SerializeField] private float catchDistance = 1.2f;
//    [SerializeField] private AudioSource moveSound;
//    [SerializeField] private Animator animator;          // optional, freezes while seen
//    [SerializeField] private bool debugSeen = false;     // tick to see Seen/Not seen in the Console

//    public bool IsLocked { get; private set; }

//    private NavMeshAgent agent;
//    private Camera playerCam;
//    private Transform player;
//    private Plane[] frustumPlanes = new Plane[6];
//    private bool lastSeen;

//    private void Start()
//    {
//        agent = GetComponent<NavMeshAgent>();

//        // Find the camera: the MainCamera tag first, then any camera in the scene
//        playerCam = Camera.main;
//        if (playerCam == null) playerCam = FindFirstObjectByType<Camera>();
//        if (playerCam == null) Debug.LogError("WeepingAngelAI: no camera found!");

//        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
//        if (playerObj != null) player = playerObj.transform;

//        if (angelRenderer == null) angelRenderer = GetComponentInChildren<Renderer>();
//        if (animator == null) animator = GetComponentInChildren<Animator>();

//        // Ensure the audio source is set up for chasing
//        if (moveSound != null)
//        {
//            moveSound.loop = true; // Make sure the chase sound loops continuously while chasing
//        }
//    }

//    private void Update()
//    {
//        if (IsLocked || player == null) return;

//        bool isSeen = IsSeenByPlayer();

//        if (debugSeen && isSeen != lastSeen)
//        {
//            Debug.Log(name + (isSeen ? " SEEN - frozen" : " NOT seen - chasing"));
//            lastSeen = isSeen;
//        }

//        if (isSeen)
//        {
//            // FREEZE: player is looking at it
//            agent.isStopped = true;
//            agent.velocity = Vector3.zero;
//            if (animator != null) animator.speed = 0f;          // pause the animation
//            if (moveSound != null && moveSound.isPlaying) moveSound.Stop();
//        }
//        else
//        {
//            // CHASE: player is not looking
//            agent.isStopped = false;
//            agent.SetDestination(player.position);
//            if (animator != null) animator.speed = 1f;          // resume the animation
//            if (moveSound != null && !moveSound.isPlaying) moveSound.Play();

//            // ATTACK CHECK: only when very close AND not seen
//            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
//            if (distanceToPlayer <= catchDistance)
//            {
//                if (Level5Manager.Instance != null)
//                    Level5Manager.Instance.PlayerCaught();
//                else
//                    Debug.LogError("Level5Manager.Instance is NULL!");
//            }
//        }
//    }



//    private bool IsSeenByPlayer()
//    {
//        if (playerCam == null || angelRenderer == null) return false;

//        // 1. Is any part of the angel inside the camera's view?
//        GeometryUtility.CalculateFrustumPlanes(playerCam, frustumPlanes);
//        Bounds b = angelRenderer.bounds;
//        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, b))
//            return false;

//        // 2. Can the camera actually see it, with no wall in between?
//        //    We check the middle, upper and lower body.
//        Vector3[] points =
//        {
//            b.center,
//            b.center + Vector3.up * b.extents.y * 0.8f,
//            b.center - Vector3.up * b.extents.y * 0.8f
//        };

//        foreach (Vector3 p in points)
//        {
//            if (!Physics.Linecast(playerCam.transform.position, p, obstructionMask))
//                return true;   // at least one point is visible
//        }

//        return false;          // every point is blocked by a wall
//    }

//}

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WeepingAngelAI : MonoBehaviour
{
    [SerializeField] private Renderer angelRenderer;      // the mesh used for the seen check
    [SerializeField] private LayerMask obstructionMask;  // Walls layer ONLY
    [SerializeField] private float catchDistance = 1.2f;
    [SerializeField] private AudioSource moveSound;      // Chasing / moving sound
    [SerializeField] private Animator animator;          // optional, freezes while seen
    [SerializeField] private bool debugSeen = false;     // tick to see Seen/Not seen in the Console

    public bool IsLocked { get; private set; }

    private NavMeshAgent agent;
    private Camera playerCam;
    private Transform player;
    private Plane[] frustumPlanes = new Plane[6];

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Find the camera: the MainCamera tag first, then any camera in the scene
        playerCam = Camera.main;
        if (playerCam == null) playerCam = FindFirstObjectByType<Camera>();
        if (playerCam == null) Debug.LogError("WeepingAngelAI: no camera found!");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (angelRenderer == null) angelRenderer = GetComponentInChildren<Renderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (moveSound != null)
        {
            moveSound.loop = true;
            moveSound.playOnAwake = false;
            moveSound.Stop(); // Ensure it starts completely silent
        }
    }

    private void Update()
    {
        if (IsLocked || player == null)
        {
            StopChaseSound();
            return;
        }

        bool isSeen = IsSeenByPlayer();

        if (isSeen)
        {
            // FREEZE: player is looking at it
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            if (animator != null) animator.speed = 0f;

            // Stop sound immediately when frozen
            StopChaseSound();
        }
        else
        {
            // CHASE: player is not looking
            agent.isStopped = false;
            agent.SetDestination(player.position);
            if (animator != null) animator.speed = 1f;

            // Play sound ONLY while actively chasing
            PlayChaseSound();

            // ATTACK CHECK: only when very close AND not seen
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= catchDistance)
            {
                if (Level5Manager.Instance != null)
                    Level5Manager.Instance.PlayerCaught();
                else
                    Debug.LogError("Level5Manager.Instance is NULL!");
            }
        }
    }

    private void PlayChaseSound()
    {
        if (moveSound != null && !moveSound.isPlaying)
        {
            moveSound.Play();
        }
    }

    private void StopChaseSound()
    {
        if (moveSound != null && moveSound.isPlaying)
        {
            moveSound.Stop();
        }
    }

    private bool IsSeenByPlayer()
    {
        if (playerCam == null || angelRenderer == null) return false;

        // 1. Is any part of the angel inside the camera's view?
        GeometryUtility.CalculateFrustumPlanes(playerCam, frustumPlanes);
        Bounds b = angelRenderer.bounds;
        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, b))
            return false;

        // 2. Can the camera actually see it, with no wall in between?
        Vector3[] points =
        {
            b.center,
            b.center + Vector3.up * b.extents.y * 0.8f,
            b.center - Vector3.up * b.extents.y * 0.8f
        };

        foreach (Vector3 p in points)
        {
            if (!Physics.Linecast(playerCam.transform.position, p, obstructionMask))
                return true;   // at least one point is visible
        }

        return false;          // every point is blocked by a wall
    }
}
