
using UnityEngine;

public class LaserPuzzle : MonoBehaviour
{
    public float maxDistance = 20f;
    public int maxBounces = 5;

    public Transform exitDoor;
    public float doorOpenHeight = 3f;
    public float doorSpeed = 2f;

    LineRenderer laser;
    Vector3 closedPosition;
    bool unlocked;

    private void Start()
    {
        laser = GetComponent<LineRenderer>();
        laser.useWorldSpace = true;

        closedPosition = exitDoor.position;
    }

    private void Update()
    {
        //laser starting position and direction
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;

        laser.positionCount = 1;
        laser.SetPosition(0, origin);

        //raycasting and mirror reflection
        for (int i = 0; i <= maxBounces; i++)
        {
            RaycastHit hit;

            if (Physics.Raycast(origin, direction, out hit, maxDistance))
            {
                laser.positionCount++;
                laser.SetPosition(laser.positionCount - 1, hit.point);

                //unlock door when laser reaches receiver
                if (hit.collider.CompareTag("Receiver"))
                {
                    unlocked = true;
                    break;
                }

                //reflect laser when hitting mirror
                if (hit.collider.CompareTag("Mirror"))
                {
                    direction = Vector3.Reflect(direction, hit.normal);
                    origin = hit.point + direction * 0.02f;
                    continue;
                }

                break;
            }

            //stop laser when nothing is hit
            laser.positionCount++;
            laser.SetPosition(laser.positionCount - 1, origin + direction * maxDistance);
            break;
        }

        //open door when puzzle is completed
        if (unlocked)
        {
            exitDoor.position = Vector3.MoveTowards(
                exitDoor.position,
                closedPosition + Vector3.up * doorOpenHeight,
                doorSpeed * Time.deltaTime

            );
            Debug.Log("Exit Door Opened");
        }
    }
}
