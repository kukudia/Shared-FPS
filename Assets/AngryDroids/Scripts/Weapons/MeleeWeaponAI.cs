using UnityEngine;

namespace GravityBox.AngryDroids
{
    public class MeleeWeaponAI : WeaponAI
    {
        IHealth health;
        public float damage;
        public float damageInterval;
        float lastDamage;

        public override void Fire()
        {
            base.Fire();

            if (health == null) return;
            if (Time.time > lastDamage + damageInterval)
            {
                health.OnDamage(new DamageInfo(DamageType.Melee, damage, transform.position));
                lastDamage = Time.time;
            }
        }

        public override bool IsInRange(Transform target)
        {
            Vector3 targetPosition = target.position;
            targetPosition = targetPosition - weaponTransform.position;

            if (targetPosition.magnitude <= rangeMax)
                health = target.GetComponent<IHealth>();
            else
            {
                health = null;
                lastDamage = -damageInterval;
            }

            return health != null;
        }

        void OnDrawGizmosSelected()
        {
            if (weaponTransform != null)
                Gizmos.DrawWireSphere(weaponTransform.position, rangeMax);
        }
    }
}