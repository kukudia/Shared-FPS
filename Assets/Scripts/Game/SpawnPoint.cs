using UnityEngine;

namespace Projectiles
{
	/// <summary>
	/// Component used for spawn point lookup in the gameplay scene.
	/// </summary>
	public sealed class SpawnPoint : MonoBehaviour
	{
		public bool isOccupied = false;

        private void OnDrawGizmos()
        {
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(transform.position, 0.3f);
        }
    }
}
