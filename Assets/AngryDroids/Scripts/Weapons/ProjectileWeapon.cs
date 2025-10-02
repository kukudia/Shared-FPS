using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public class ProjectileWeapon : Weapon
    {
        [SerializeField]
        protected GameObject projectilePrefab;
        protected SmallObjectPool projectiles;

        public Transform[] spawnPositions;
        public GameObject muzzle;
        public float projectileDelay = 0.3f;

        int _next = 0;

        protected override void OnAwake()
        {
            projectiles = new SmallObjectPool(projectilePrefab);
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