using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public class ProjectileWeaponAI : WeaponAI, IWeapon
    {
        [SerializeField]
        protected GameObject projectilePrefab;
        protected SmallObjectPool projectiles;

        public Transform[] spawnPositions;
        public GameObject muzzle;
        public float projectileDelay = 0.3f;

        private int _next = 0;
        private Transform _target;

        protected override void OnAwake()
        {
            projectiles = new SmallObjectPool(projectilePrefab);
        }

        public override bool CanFire()
        {
            return !isFiring && !IsCoolingDown();
        }

        public override void Fire(Transform target)
        {
            _target = target;
            base.Fire(_target);
        }

        protected override IEnumerator FireCoroutine()
        {
            Transform spawnAt = Next();
            if (muzzle != null)
            {
                muzzle.transform.position = spawnAt.position;
                muzzle.SetActive(true);
            }
            yield return new WaitForSeconds(projectileDelay);
            GameObject bullet = projectiles.Get();
            bullet.transform.position = spawnAt.position;
            bullet.transform.rotation = spawnAt.rotation;

            if (_target != null && bullet.GetComponent<ITargetable>() != null)
                bullet.GetComponent<ITargetable>().SetTarget(_target.gameObject);
            _target = null;

            bullet.SetActive(true);

            isFiring = false;
        }

        protected Transform Next() 
        {
            Transform result = spawnPositions[_next]; 
            _next = (_next + 1) % spawnPositions.Length;
            return result;
        }
    }
}