using UnityEngine;

namespace Backrooms.Core
{
    // goes on the player's camera.
    // left click shoots a raycast from the middle of the screen (the crosshair).
    // if it hits something shootable within range, that object's OnShot() runs.
    [RequireComponent(typeof(Camera))]
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private float range = 20f;          // how far the shot reaches
        [SerializeField] private float fireCooldown = 0.2f;  // seconds between shots
        [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;   // skips the "ignore raycast" layer
        [SerializeField] private AudioSource shotAudio;
        [SerializeField] private bool drawDebugRay = true;   // shows the shot line in the scene view

        private Camera cam;
        private AudioClip shotClip;
        private float nextFireTime;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            shotClip = ToneGenerator.CreateNoiseBurst("Shot", 0.12f);   // gunshot sound made in code
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;              // game is paused
            if (!Input.GetMouseButtonDown(0)) return;      // no left click this frame
            if (Time.time < nextFireTime) return;          // still cooling down

            nextFireTime = Time.time + fireCooldown;
            Fire();
        }

        private void Fire()
        {
            if (shotAudio != null) shotAudio.PlayOneShot(shotClip);

            // (0.5, 0.5) = exact centre of the screen = where the crosshair is
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (drawDebugRay) Debug.DrawLine(ray.origin, hit.point, Color.green, 0.5f);

                // check the hit object (or its parents) for IShootable.
                // this way the collider can be on a child, like the target disc.
                IShootable target = hit.collider.GetComponentInParent<IShootable>();
                if (target != null) target.OnShot(hit);
            }
            else if (drawDebugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 0.5f);   // missed
            }
        }
    }
}
