using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public interface ITargetable
    {
        void SetTarget(GameObject target);
    }

    public class ProjectileTargetable : Projectile, ITargetable
    {
        public GameObject trailObject;

        public bool isTargetable = true;
        public float seekPrecision = 1.3f;
        private GameObject targetObject;
        private Collider targetCollider;

        private Vector3 direction;

        protected override IEnumerator EnableCoroutine()
        {
            DisableCollision();
            Reset();

            if (isTargetable)
            {
                direction = transform.forward;
                //targetObject = GameObject.FindWithTag("Player");
                //if (targetObject != null)
                //    targetCollider = targetObject.GetComponent<Collider>();
            }

            rigidbody.velocity = transform.forward * velocity;
            yield return null;
            StartTrail();
        }

        private void StartTrail()
        {
            if (trailObject != null)
            {
                ParticleSystem[] particles = trailObject.GetComponentsInChildren<ParticleSystem>();
                ParticleSystem.MainModule main;
                foreach (ParticleSystem p in particles)
                {
                    main = p.main;
                    main.loop = true; 
                }
                main = trailObject.GetComponent<ParticleSystem>().main;
                main.loop = true;
                trailObject.transform.parent = gameObject.transform;
                trailObject.transform.localPosition = new Vector3(0, 0, -0.15f);
                trailObject.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                trailObject.SetActive(true);
            }
        }

        private void StopTrail()
        {
            if (trailObject != null)
            {
                ParticleSystem[] particles = trailObject.GetComponentsInChildren<ParticleSystem>();
                ParticleSystem.MainModule main;
                foreach (ParticleSystem p in particles)
                {
                    main = p.main;
                    main.loop = false; 
                }
                main = trailObject.GetComponent<ParticleSystem>().main;
                main.loop = false;
                trailObject.transform.parent = null;
            }
        }

        public void SetTarget(GameObject target)
        {
            targetObject = target;
            targetCollider = targetObject.GetComponent<Collider>();
        }

        protected override void Explode(Collision collision)
        {
            StopTrail();
            base.Explode(collision);
        }

        void FixedUpdate()
        {
            if (isTargetable && targetObject != null)
            {
                //move like a homing missile
                Vector3 targetPos = targetCollider.bounds.center;
                targetPos += transform.right * (Mathf.PingPong(Time.time, 1.0f) - 0.5f) * 0.1f; //noise
                Vector3 targetDir = (targetPos - transform.position);
                float targetDist = targetDir.magnitude;
                targetDir /= targetDist;

                if (targetDist < 1f)
                    targetDir += Vector3.down * 0.5f;

                direction = Vector3.Slerp(direction, targetDir, Time.deltaTime * seekPrecision);
                transform.rotation = Quaternion.LookRotation(direction);
                rigidbody.velocity = direction.normalized * velocity;
            }
        }
    }
}