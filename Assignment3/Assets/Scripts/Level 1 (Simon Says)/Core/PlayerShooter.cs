using UnityEngine;

namespace Backrooms.Core
{
    /// <summary>
    /// Goes on the player's camera. Left click fires a raycast from the centre of the screen
    /// (where the crosshair is). If it hits something with an IShootable component within range,
    /// that object's OnShot() is called.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private float range = 20f;
        [SerializeField] private float fireCooldown = 0.2f;
        [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;
        [SerializeField] private AudioSource shotAudio;
        [SerializeField] private bool drawDebugRay = true;

        private Camera cam;
        private AudioClip shotClip;
        private float nextFireTime;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            shotClip = ToneGenerator.CreateNoiseBurst("Shot", 0.12f);
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;              // game is paused
            if (!Input.GetMouseButtonDown(0)) return;
            if (Time.time < nextFireTime) return;          // still cooling down

            nextFireTime = Time.time + fireCooldown;
            Fire();
        }

        private void Fire()
        {
            if (shotAudio != null) shotAudio.PlayOneShot(shotClip);

            // (0.5, 0.5) in viewport space = exact centre of the screen = the crosshair
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (drawDebugRay) Debug.DrawLine(ray.origin, hit.point, Color.green, 0.5f);

                // GetComponentInParent so the collider can sit on a child (e.g. the target disc)
                IShootable target = hit.collider.GetComponentInParent<IShootable>();
                if (target != null) target.OnShot(hit);
            }
            else if (drawDebugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 0.5f);
            }
        }
    }
}
