using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public class Grenage : Projectile, ITargetable
    {
        public float explosionDelay = 1f;
        [SerializeField]
        private GameObject targetObject;

        public void SetTarget(GameObject target)
        {
            targetObject = target;
        }

        protected override IEnumerator EnableCoroutine()
        {
            DisableCollision();
            Reset();

            float calculatedVelocity = targetObject ? CalculateVelocity(targetObject.transform.position - transform.position) : velocity; //Mathf.Sqrt(velocitySqr);
            velocity = calculatedVelocity;
            rigidbody.velocity = transform.forward * calculatedVelocity;

            GetComponent<TrailRenderer>().enabled = true;
            yield return new WaitForSeconds(activationTime);
        }

        float CalculateVelocity(Vector3 direction)
        {
            float yOffset = -direction.y;

            direction.y = 0;
            float distance = direction.magnitude;
            Vector3 horizontalDirection = transform.forward;
            horizontalDirection.y = 0;
            horizontalDirection.Normalize();
            float angle = Vector3.Angle(transform.forward, horizontalDirection);
            if (angle == 0)
                angle = 1;
            else
                angle *= Mathf.Deg2Rad;
            float result = (1f / Mathf.Cos(angle)) * Mathf.Sqrt((1f * 9.81f * distance * distance) / (distance * Mathf.Tan(angle) + yOffset));
            return result;
        }

        protected override void Explode(Collision collision)
        {
            float delay = collision.collider.GetComponent<IHealth>() != null ? 0 : explosionDelay;

            StartCoroutine(ExplodeCoroutine(delay));
        }

        private IEnumerator ExplodeCoroutine(float delay = 0)
        {
            GetComponent<TrailRenderer>().enabled = false;
            yield return new WaitForSeconds(delay);
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            DisableCollision();
            gameObject.SetActive(false);
        }
    }
}