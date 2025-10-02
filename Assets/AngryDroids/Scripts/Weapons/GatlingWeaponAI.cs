using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    [System.Serializable]
    public class GunBarrel 
    {
        public Transform barrel;
        public Transform muzzlePoint;
        public Transform smokePoint;

        private ParticleSystem muzzle;
        private ParticleSystem smoke;

        ParticleSystem[] shellsSubParticles;
        private RaycastHit hit;
        private DamageInfo damage;

        public void Init(ParticleSystem muzzlePrefab, ParticleSystem smokePrefab) 
        {
            if (muzzlePrefab != null)
            {
                muzzle = Object.Instantiate(muzzlePrefab, muzzlePoint.position, muzzlePoint.rotation);
                muzzle.transform.parent = barrel.parent;
                smoke = Object.Instantiate(smokePrefab, smokePoint.position, smokePoint.rotation);
                smoke.transform.parent = barrel.parent;
            }

            shellsSubParticles = muzzle.GetComponentsInChildren<ParticleSystem>(true);
        }

        public void Fire(GatlingWeaponAI gun, SmallObjectPool pool) 
        {
            SetEffect();
            
            Vector3 forward = barrel.forward + Random.onUnitSphere * gun.offset;

            if (Physics.Raycast(barrel.position, forward, out hit, gun.rangeMax, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                IHealth health = hit.collider.GetComponent<IHealth>();
                if (health != null)
                    health.OnDamage(new DamageInfo(DamageType.Bullet, gun.damagePerShot, gun.transform.forward));

                GameObject hitp = pool.Get();
                hitp.transform.position = hit.point;
                hitp.transform.forward = hit.normal;
                hitp.SetActive(true);
            }
        }

        void SetEffect()
        {
            smoke.gameObject.SetActive(true);
            var main = smoke.main;
            if (!main.loop)
                main.loop = true;

            if (!muzzle.gameObject.activeInHierarchy)
                muzzle.gameObject.SetActive(true);
            else
            {
                foreach (ParticleSystem p in shellsSubParticles)
                    p.Emit(1);
            }
        }

        public void Stop() 
        {
            var main = smoke.main;
            main.loop = false;
        }
    }

    public class GatlingWeaponAI : WeaponAI, IWeapon 
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
            foreach (GunBarrel b in barrels)
                b.Fire(this, hits);

            isFiring = false;
            
            yield return new WaitForSeconds(0.1f);
            fireLight.intensity = 0;
            foreach (GunBarrel b in barrels)
                b.Stop();
        }
    }
}
