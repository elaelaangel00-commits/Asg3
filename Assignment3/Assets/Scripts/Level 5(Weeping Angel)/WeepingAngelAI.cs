using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WeepingAngelAI : MonoBehaviour
{
    [SerializeField] private Renderer angelRenderer;     // the mesh used for the seen check
    [SerializeField] private LayerMask obstructionMask;  // Walls layer
    [SerializeField] private float catchDistance = 1.2f;
    [SerializeField] private AudioSource moveSound;

    public bool IsLocked { get; private set; }

    private NavMeshAgent agent;
    private Camera playerCam;
    private Transform player;
    private Plane[] frustumPlanes = new Plane[6];

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        playerCam = Camera.main;
        //player = GameObject.FindGameObjectWithTag("Player").transform;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        if (angelRenderer == null) angelRenderer = GetComponentInChildren<Renderer>();
    }


    private void Update()
    {
        if (IsLocked || player == null) return;

        bool isSeen = IsSeenByPlayer();

        if (isSeen)
        {
            // FREEZE: Player is looking at it
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            if (moveSound != null && moveSound.isPlaying) moveSound.Stop();
        }
        else
        {
            // CHASE: Player is not looking
            agent.isStopped = false;
            agent.SetDestination(player.position);
            if (moveSound != null && !moveSound.isPlaying) moveSound.Play();

            // ATTACK CHECK: Only attacks when very close AND not seen
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            Debug.Log("Distance to player: " + distanceToPlayer);
            if (distanceToPlayer <= catchDistance)
            {
                Debug.Log("Distance to player: " + distanceToPlayer);
                if (Level5Manager.Instance != null)
                {
                    Level5Manager.Instance.PlayerCaught();
                }
                else
                {
                    Debug.LogError("Level5Manager.Instance is NULL!");
                }
            }
        }
    }



    private bool IsSeenByPlayer()
    {
        if (playerCam == null || angelRenderer == null) return false;

        GeometryUtility.CalculateFrustumPlanes(playerCam, frustumPlanes);
        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, angelRenderer.bounds))
            return false;                           // Outside the view

        Vector3 target = angelRenderer.bounds.center;
        if (Physics.Linecast(playerCam.transform.position, target, obstructionMask))
            return false;                           // A wall blocks the view

        return true;
    }

    public void LockInCell(Transform lockPoint)
    {
        IsLocked = true;
        agent.enabled = false;                             // stop the NavMeshAgent
        transform.SetPositionAndRotation(lockPoint.position, lockPoint.rotation);
        if (moveSound != null) moveSound.Stop();
        if (Level5Manager.Instance != null)
            Level5Manager.Instance.RegisterLockedAngel();
    }


}