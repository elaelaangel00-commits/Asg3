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
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (angelRenderer == null) angelRenderer = GetComponentInChildren<Renderer>();
    }

    private void Update()
    {
        if (IsLocked) return;

        if (IsSeenByPlayer())
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            if (moveSound != null && moveSound.isPlaying) moveSound.Stop();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            if (moveSound != null && !moveSound.isPlaying) moveSound.Play();

            if (Vector3.Distance(transform.position, player.position) <= catchDistance)
            {
                if (Level5Manager.Instance != null)
                    Level5Manager.Instance.PlayerCaught();
            }
        }
    }

    // Seen = inside the camera's view AND nothing solid in between
    private bool IsSeenByPlayer()
    {
        GeometryUtility.CalculateFrustumPlanes(playerCam, frustumPlanes);
        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, angelRenderer.bounds))
            return false;                                  // outside the view

        Vector3 target = angelRenderer.bounds.center;
        if (Physics.Linecast(playerCam.transform.position, target, obstructionMask))
            return false;                                  // a wall blocks the view

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