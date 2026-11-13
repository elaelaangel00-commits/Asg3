using UnityEngine;

namespace Backrooms.Core
{
    // anything the player can shoot.
    // PlayerShooter looks for this on whatever the raycast hits,
    // so targets and buttons can react in their own way.
    public interface IShootable
    {
        void OnShot(RaycastHit hit);
    }
}
