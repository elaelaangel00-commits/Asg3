using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Weeping Angel AI
// - Stays dormant (no movement, no sound) until the player has seen it once
// - Frozen while the player can see it (frustum check + line-of-sight raycast)
// - Chases with a NavMeshAgent while the player is NOT looking
// - Catches the player when close enough and unseen
[RequireComponent(typeof(NavMeshAgent))]
public class WeepingAngelAI : MonoBehaviour
{
    [Header("Wake Up")]
    [Tooltip("Tick: the angel does nothing until the player has looked at it once.")]
    [SerializeField] private bool wakeOnFirstSight = true;

    [Header("Line of Sight")]
    [Tooltip("Layers that block the player's view. Walls ONLY.")]
    [SerializeField] private LayerMask obstructionMask;

    [Tooltip("The check ray stops this far before the angel, so a wall touching its back doesn't block the view.")]
    [SerializeField] private float rayEndOffset = 0.6f;

    [Tooltip("Angels further away than this never count as seen. 0 = unlimited.")]
    [SerializeField] private float maxSeenDistance = 0f;

    [Header("Chase")]
    [SerializeField] private float catchDistance = 1.2f;

    [Tooltip("Stays frozen this long after you look away. Stops 1-frame glitches.")]
    [SerializeField] private float unseenGraceTime = 0.15f;

    [Header("Audio / Animation")]
    [SerializeField] private AudioSource moveSound;   // chasing sound
    [SerializeField] private Animator animator;       // frozen while seen

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    public bool IsLocked { get; private set; }

    private NavMeshAgent agent;
    private Camera playerCam;
    private Transform player;

    // Reused every frame (no garbage created)
    private readonly Plane[] frustumPlanes = new Plane[6];
    private readonly Vector3[] checkPoints = new Vector3[3];

    private readonly List<Renderer> bodyRenderers = new List<Renderer>();
    private float lastSeenTime = -999f;
    private bool hasWoken;         // has the player seen this angel at least once?
    private bool wasFrozen;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Find the camera: the MainCamera tag first, then any camera in the scene
        playerCam = Camera.main;
        if (playerCam == null) playerCam = FindFirstObjectByType<Camera>();
        if (playerCam == null) Debug.LogError("WeepingAngelAI: no camera found!");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;   // the agent moves it, not the animation

        // Collect EVERY mesh of the angel, so the whole body counts as "seen"
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            if (r is MeshRenderer || r is SkinnedMeshRenderer) bodyRenderers.Add(r);

        if (moveSound != null)
        {
            moveSound.loop = true;
            moveSound.playOnAwake = false;
            moveSound.Stop();
        }

        // Start completely still and silent
        Freeze();
    }

    private void Update()
    {
        // Game paused (intro popup, game over...): nothing moves, nothing plays
        if (Time.timeScale == 0f)
        {
            StopChaseSound();
            return;
        }

        if (IsLocked || player == null)
        {
            StopChaseSound();
            return;
        }

        // Seen now, OR seen a split second ago (grace time)
        if (IsSeenByPlayer()) lastSeenTime = Time.time;
        bool isSeen = (Time.time - lastSeenTime) < unseenGraceTime;

        // The first time the player looks at it, the angel wakes up
        if (isSeen) hasWoken = true;

        if (debugLog && isSeen != wasFrozen)
        {
            Debug.Log(name + (isSeen ? " FROZEN" : " NOT SEEN") + " (woken: " + hasWoken + ")");
            wasFrozen = isSeen;
        }

        if (isSeen)
            Freeze();                                  // being watched: zero movement
        else if (wakeOnFirstSight && !hasWoken)
            Freeze();                                  // never seen yet: stay dormant
        else
            Chase();                                   // woken and not watched: hunt the player
    }

    // ---------- States ----------

    private void Freeze()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        if (animator != null) animator.speed = 0f;
        StopChaseSound();
    }

    private void Chase()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        if (animator != null) animator.speed = 1f;
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

    // ---------- Sound ----------

    private void PlayChaseSound()
    {
        if (moveSound != null && !moveSound.isPlaying) moveSound.Play();
    }

    private void StopChaseSound()
    {
        if (moveSound != null && moveSound.isPlaying) moveSound.Stop();
    }

    // ---------- Visibility ----------

    // The box that wraps the WHOLE angel (body, head, axe, hat...)
    private bool GetBodyBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        bool found = false;

        foreach (Renderer r in bodyRenderers)
        {
            if (r == null || !r.gameObject.activeInHierarchy) continue;

            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return found;
    }

    private bool IsSeenByPlayer()
    {
        if (playerCam == null) return false;
        if (!GetBodyBounds(out Bounds b)) return false;

        Vector3 camPos = playerCam.transform.position;

        // 0. Too far away to be seen? (optional)
        if (maxSeenDistance > 0f && Vector3.Distance(camPos, b.center) > maxSeenDistance)
            return false;

        // 1. Frustum check: is any part of the angel inside the camera's view?
        GeometryUtility.CalculateFrustumPlanes(playerCam, frustumPlanes);
        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, b))
            return false;

        // 2. Line-of-sight raycast: is a wall between the camera and the angel?
        checkPoints[0] = b.center;
        checkPoints[1] = b.center + Vector3.up * b.extents.y * 0.8f;
        checkPoints[2] = b.center - Vector3.up * b.extents.y * 0.8f;

        foreach (Vector3 p in checkPoints)
        {
            Vector3 end = Vector3.MoveTowards(p, camPos, rayEndOffset);

            if (!Physics.Linecast(camPos, end, obstructionMask, QueryTriggerInteraction.Ignore))
                return true;   // at least one point is visible
        }

        return false;          // every point is blocked by a wall
    }
}

