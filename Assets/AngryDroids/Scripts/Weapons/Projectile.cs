using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public class Projectile : MonoBehaviour
    {
        public GameObject explosionPrefab;

        public float velocity = 100f;
        public float activationRange = 0.5f;
        public float activationTime = 0.1f;
        public float destructionRange = 100f;
        public float destructionTime = 100f;

        private float lifeDistance = 0;
        private float lifeTime = 0;
        private Vector3 spawnPosition = Vector3.zero;

        private Collider _collider;
        public new Collider collider
        {
            get
            {
                if (_collider == null)
                    _collider = GetComponentInChildren<Collider>();

                return _collider;
            }
        }

        private Rigidbody _rigidbody;
        public new Rigidbody rigidbody
        {
            get
            {
                if (_rigidbody == null)
                    _rigidbody = GetComponent<Rigidbody>();

                return _rigidbody;
            }
        }

        void OnEnable()
        {
            StartCoroutine(EnableCoroutine());
        }

        protected virtual IEnumerator EnableCoroutine()
        {
            yield return null;
        }

        void Update()
        {
            lifeDistance = Vector3.Distance(rigidbody.position, spawnPosition);
            lifeTime += Time.deltaTime;
            if (!collider.enabled)
            {
                if (lifeDistance > activationRange || lifeTime > activationTime)
                {
                    collider.gameObject.layer = 2;
                    collider.enabled = true;
                }
            }

            if (lifeDistance > destructionRange || lifeTime > destructionTime)
                Explode(null);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.isTrigger)
                return;

            Explode(collision);
        }

        protected virtual void Explode(Collision collision)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            DisableCollision();
            gameObject.SetActive(false);
        }

        protected void EnableCollision()
        {
            if (collider.enabled == false)
            {
                collider.gameObject.layer = 2;
                collider.enabled = true;
            }
        }

        protected void DisableCollision()
        {
            if (collider.enabled == true)
            {
                collider.gameObject.layer = 1;
                collider.enabled = false;
            }
        }

        protected void Reset()
        {
            lifeDistance = 0;
            lifeTime = 0;
            spawnPosition = transform.position;
        }
    }
}