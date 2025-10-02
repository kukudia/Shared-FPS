using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public class GatlingWeapon : Weapon 
    {
        public float damagePerShot = 1f;
        public float offset = 0.01f;
        public GameObject hitParticles;
        public Light fireLight;
        public GunBarrel[] barrels;
        public ParticleSystem muzzle;
        public ParticleSystem smoke;
        
        private SmallObjectPool hits;

        protected override void OnAwake()
        {
            foreach (GunBarrel b in barrels) b.Init(muzzle, smoke);
            hits = new SmallObjectPool(hitParticles, 20);
        }

        protected override IEnumerator FireCoroutine()
        {
            yield return new WaitForSeconds(0.02f);
            fireLight.intensity = Random.Range(2f, 4f);

            isFiring = false;
            
            yield return new WaitForSeconds(0.1f);
            fireLight.intensity = 0;
            foreach (GunBarrel b in barrels)
                b.Stop();
        }
    }
}