using UnityEngine;

namespace Backrooms.Core
{
    /// <summary>
    /// Anything the player's shot can hit and react to.
    /// PlayerShooter finds this interface on whatever the raycast hits.
    /// </summary>
    public interface IShootable
    {
        void OnShot(RaycastHit hit);
    }
}
