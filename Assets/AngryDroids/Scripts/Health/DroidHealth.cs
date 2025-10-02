using UnityEngine;

namespace GravityBox.AngryDroids
{
    public class DroidHealth : Health
    {
        public GameObject debris;

        protected override void OnDeath()
        {
            GameObject d = Instantiate(debris);
            d.transform.position = transform.position;
            d.transform.rotation = transform.rotation;
            d.GetComponent<DroidDestroyer>().SetTarget(transform);
            base.OnDeath();
        }

        public override void OnDamage(DamageInfo damage)
        {
            if (!IsReady()) return;

            base.OnDamage(damage);
            
            //if (!dead) //this triggers error if AI has no target
                //ai.OnSpotted();
        }

        bool IsReady() { return animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Active"); }
    }
}