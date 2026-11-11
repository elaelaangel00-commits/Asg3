using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// Weeping Angel AI (Final Room)
/// CORE IDEA: the angel only moves when the player is NOT looking at it.

/// Behaviour summary:
///  - DORMANT : does nothing until the player has seen it at least once.
///  - FROZEN  : while the player can see it (camera frustum check + wall raycast).
///  - CHASING : once woken and unseen, it follows the player using a NavMeshAgent.
///  - CAUGHT  : if it gets within catchDistance while unseen, the player loses.
///
/// Depends on:
///  - NavMeshAgent (guaranteed by [RequireComponent]); the scene needs a baked NavMesh.
///  - A GameObject tagged "Player".
///  - A camera (tagged MainCamera ideally) that represents the player's eyes.
///  - Level5Manager (singleton) which handles the game-over logic.
/// </summary>

[RequireComponent(typeof(NavMeshAgent))]
public class WeepingAngelAI : MonoBehaviour
{
    // Inspector settings 

    [Header("Wake Up")]
    [Tooltip("Tick: the angel does nothing until the player has looked at it once.")]
    [SerializeField] private bool wakeOnFirstSight = true;

    [Header("Line of Sight")]
    [Tooltip("Layers that block the player's view. Walls ONLY.")]
    [SerializeField] private LayerMask obstructionMask; ///Must Not include the player or the angel itself, or it will never be seen.

    [Tooltip("The check ray stops this far before the angel, so a wall touching its back doesn't block the view.")]
    [SerializeField] private float rayEndOffset = 0.6f;

    [Tooltip("Angels further away than this never count as seen. 0 = unlimited.")]
    [SerializeField] private float maxSeenDistance = 0f;

    [Header("Chase")]
    [SerializeField] private float catchDistance = 1.2f; /// distance to the player at which the angel "catches" them (game over)

    [Tooltip("Stays frozen this long after you look away. Stops 1-frame glitches.")]
    [SerializeField] private float unseenGraceTime = 0.15f;

    [Header("Audio / Animation")]
    [SerializeField] private AudioSource moveSound;   // chasing sound
    [SerializeField] private Animator animator;       // frozen while seen

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;




    /// Public property to lock the angel in place (frozen) regardless of whether the player is looking at it or not.
    /// When true, Update() does nothing and the angel is silent.
    /// NOTE: only settable from inside this class, and nothing in this script sets it.
    public bool IsLocked { get; private set; }


    /// private reference to the NavMeshAgent component, used for pathfinding and movement.
    private NavMeshAgent agent; /// hanles pathfinding and movement
    private Camera playerCam; /// the player's camera (eyes), used to determine if the angel is seen
    private Transform player; /// the chase target 


    // Arrays are allocated ONCE and reused every frame, so Update() creates no garbage
    private readonly Plane[] frustumPlanes = new Plane[6]; /// the 6 sides of the camera view volume 
    private readonly Vector3[] checkPoints = new Vector3[3]; //3 points on the angel's body to check for line-of-sight raycasts (center, top, bottom)
    private readonly List<Renderer> bodyRenderers = new List<Renderer>(); ///every mesh of the angel, so the whole body counts as "seen"


    private float lastSeenTime = -999f;
    private bool hasWoken;         /// has the player seen this angel at least once?
    private bool wasFrozen;       ///  previous seen state, only used to avoid spamming debug logs when the state doesn't change


    ///Set Up
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Find the camera: the MainCamera tag first, then any camera in the scene
        playerCam = Camera.main;
        if (playerCam == null) playerCam = FindFirstObjectByType<Camera>();
        if (playerCam == null) Debug.LogError("WeepingAngelAI: no camera found!");

        // Find the player: the Player tag first, then any object with a Transform
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        // Animator is optional, but if present it should be disabled while the angel is frozen
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;   // the agent moves it, not the animation

        // Collect EVERY mesh of the angel, so the whole body counts as "seen"
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            if (r is MeshRenderer || r is SkinnedMeshRenderer) bodyRenderers.Add(r);

        // Prepare the chase sound: looped, not playing on awake, and stopped
        if (moveSound != null)
        {
            moveSound.loop = true;
            moveSound.playOnAwake = false;
            moveSound.Stop();
        }

        // Start completely still and silent
        Freeze();
    }


    // Main loop: check if the angel is seen, and either freeze or chase the player
    private void Update()
    {
        // Game paused (intro popup, game over...): nothing moves, nothing plays
        if (Time.timeScale == 0f)
        {
            StopChaseSound();
            return;
        }

        //diabled or no player: nothing moves, stay silent
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

        // debug log: only print when the state changes, not every frame
        if (debugLog && isSeen != wasFrozen)
        {
            Debug.Log(name + (isSeen ? " FROZEN" : " NOT SEEN") + " (woken: " + hasWoken + ")");
            wasFrozen = isSeen;
        }

        //decide what to do: freeze or chase
        if (isSeen)
            Freeze();                                  // being watched: zero movement
        else if (wakeOnFirstSight && !hasWoken)
            Freeze();                                  // never seen yet: stay dormant
        else
            Chase();                                   // woken and not watched: hunt the player
    }

    // ---------- States ----------
    // Stop all movememt and sound, and freeze the animator if present. The angel is completely still.
    private void Freeze()
    {
        //isOnNavMesh guard avoids errors if the angel isn't placed on the baked NavMesh
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;         /// stop following the path, but keep the agent enabled so it can resume later
            agent.velocity = Vector3.zero;  /// kill leftover momentum, so it doesn't slide a bit after being frozen
        }
        if (animator != null) animator.speed = 0f; ///speed 0 = pose is held (the statue look)
        StopChaseSound();
    }

  
    /// Move toward the player using the NavMeshAgent. If it gets very close, trigger the "caught" event.  
    private void Chase()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);   ///re-path to the player current position every frame
        if (animator != null) animator.speed = 1f; // resume animation 
        PlayChaseSound();

        // ATTACK CHECK: ONLY REACHABLE FROM CHASE(), so it can only happen while not seen 
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= catchDistance)
        {    /// Null-check because Level5Manager is a singleton that might be missing from the scene
            if (Level5Manager.Instance != null)
                Level5Manager.Instance.PlayerCaught();
        }
    }


    /// Sound
    /// the IsPlaying check avoids restarting the sound every frame, which would cause clicks and pops
    private void PlayChaseSound()
    {
        if (moveSound != null && !moveSound.isPlaying) moveSound.Play();
    }

    private void StopChaseSound()
    {
        if (moveSound != null && moveSound.isPlaying) moveSound.Stop();
    }

    /// Visibility "can player see this angels"
    /// Builds one box (Bounds) that wraps ALL of the angel's meshes, return false if no meshes are found (e.g. the angel is disabled)

    private bool GetBodyBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        bool found = false;

        foreach (Renderer r in bodyRenderers)
        {
            if (r == null || !r.gameObject.activeInHierarchy) continue;

            if (!found) { bounds = r.bounds; found = true; } /// first mesh found, initialize the bounds to it
            else bounds.Encapsulate(r.bounds); ///later meshes: expand the bounds to include them
        }
        return found;
    }

    /// Two checks to see if the angel is currently visible to the player:
    /// Two-step test. Step 1 (cheap): is the angel inside the camera's view cone?
    /// Step 2 (more expensive): is there a wall in between?
    /// Both must pass for the angel to count as "seen".

    private bool IsSeenByPlayer()
    {
        if (playerCam == null) return false;
        if (!GetBodyBounds(out Bounds b)) return false;

        Vector3 camPos = playerCam.transform.position;

        /// 0. Too far away to be seen? (optional, disabled when maxSeenDistance = 0)
        if (maxSeenDistance > 0f && Vector3.Distance(camPos, b.center) > maxSeenDistance)
            return false;

        /// 1. Frustum check: is any part of the angel inside the camera's view?
        ///   The frustum is the pyramid-shaped volume the camera can see.
        ///   (Fast math, so we do it first and skip the raycasts when it fails.)
        GeometryUtility.CalculateFrustumPlanes(playerCam, frustumPlanes);
        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, b))
            return false;

        /// 2. Line-of-sight raycast: is a wall between the camera and the angel?
         ///    We test 3 points (middle, upper, lower) so the angel counts as seen even if
        ///   only its head or feet peek out from behind a wall/corner.
        checkPoints[0] = b.center;
        checkPoints[1] = b.center + Vector3.up * b.extents.y * 0.8f;
        checkPoints[2] = b.center - Vector3.up * b.extents.y * 0.8f;

        foreach (Vector3 p in checkPoints)
        {
            /// End the ray slightly in front of the point (toward the camera) so a wall
            /// directly behind the angel doesn't wrongly block the ray.
            Vector3 end = Vector3.MoveTowards(p, camPos, rayEndOffset);


            /// Linecast returns TRUE if it HITS something. So "!Linecast" = clear view.
            /// The mask only has walls, so the player/angel colliders are ignored.
            if (!Physics.Linecast(camPos, end, obstructionMask, QueryTriggerInteraction.Ignore))
                return true;   // at least one point is visible
        }

        return false;          // every point is blocked by a wall
    }
}

