using UnityEngine;

namespace GravityBox.AngryDroids
{
    public class AISight : MonoBehaviour
    {
        public Transform sight;

        public float FOV = 110f;
        public float rangeMax = 1000f;
        public float rangeMin = 0f;
        public float hearingRange = 30;

        private RaycastHit hit;
        private Collider playerCollider;
        private Transform _player;
        
        public Transform player
        {
            get { return _player; }
            set 
            {
                playerCollider = value ? value.GetComponent<Collider>() : null;
                _player = value;
            }
        }

        public Vector3 playerDirection { get { return playerCollider.bounds.center - sight.position; } }
        public Vector3 playerDirectionHorizontal 
        { 
            get 
            {
                Vector3 direction = _player.position - sight.position;
                direction.y = 0;
                return direction;
            }
        }
        
        public Vector3 targetPosition { get; set; }
        public Vector3 lastTargetPosition { get; set; }

        public bool CanSee(Collider target) 
        {
            Bounds bounds = target.bounds;
            if (!Sight.IsVisible(bounds, sight, FOV, rangeMin, rangeMax))
                return false;

            Vector3 playerDirection = (bounds.center - sight.position);
            Physics.Raycast(sight.position, playerDirection, out hit, playerDirection.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hit.collider && hit.collider.transform == target.transform)
                return true;

            return false;
        }

        public bool CanSeeTargetFromPoint(Collider target) { return Sight.IsVisibleFromPoint(sight.position, target); }
        public Vector3 GetTargetDirection(Collider target) { return target.bounds.center - sight.position; }
        public Vector3 GetTargetGroundDirection(Transform target) 
        {
            Vector3 dir = target.position - sight.position;
            dir.y = 0;
            return dir;
        }
        public Vector3 GetPlayerNearestDirection(float offsetFromTarget)
        {
            return playerDirection - playerDirectionHorizontal.normalized * offsetFromTarget;
        }

        void Start()
        {
            if (sight == null) sight = transform;
        }

        void OnDrawGizmosSelected()
        {
            Color oldColor = Gizmos.color;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Vector3 pos = Vector3.zero;
            Gizmos.matrix = sight != null ? sight.localToWorldMatrix : transform.localToWorldMatrix;
            if (playerCollider != null && sight != null)
                Gizmos.color = CanSee(playerCollider) ? Color.green : Color.red;
            Gizmos.DrawFrustum(pos, FOV, rangeMax, rangeMin, 1);
            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;
        }
    }
}
