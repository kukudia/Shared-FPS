using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public enum DamageType
    {
        None, Melee, Bullet, Laser, Explosion, Electric
    }

    public class DamageInfo
    {
        public DamageType type = DamageType.None;
        public float amount;
        public Vector3 point;
        public Vector3 normal;
        public Vector3 direction;
        public GameObject instigator;

        public DamageInfo(float amount)
        {
            this.type = DamageType.None;
            this.amount = amount;
            this.point = Vector3.zero;
            this.normal = Vector3.forward;
            this.direction = Vector3.down;
            this.instigator = null;
        }

        public DamageInfo(DamageType type, float amount)
        {
            this.type = type;
            this.amount = amount;
            this.point = Vector3.zero;
            this.normal = Vector3.forward;
            this.direction = Vector3.down;
            this.instigator = null;
        }

        public DamageInfo(DamageType type, float amount, Vector3 fromDirection)
        {
            this.type = type;
            this.amount = amount;
            this.point = Vector3.zero;
            this.normal = Vector3.forward;
            this.direction = fromDirection;
            this.instigator = null;
        }

        public DamageInfo(DamageType type, float amount, Vector3 fromDirection, RaycastHit hitInfo)
        {
            this.type = type;
            this.amount = amount;
            this.point = hitInfo.point;
            this.normal = hitInfo.normal;
            this.direction = fromDirection;
            this.instigator = null;
        }

        public DamageInfo(DamageType type, float amount, Vector3 fromDirection, RaycastHit hitInfo, GameObject attacker)
        {
            this.type = type;
            this.amount = amount;
            this.point = hitInfo.point;
            this.normal = hitInfo.normal;
            this.direction = fromDirection;
            this.instigator = attacker;
        }
    }

    public interface IHealth 
    {
        bool Dead { get; set; }
        void OnDamage(DamageInfo damage);
    }

    public class Health : MonoBehaviour, IHealth
    {
        public event System.Action onDeath;
        public event System.Action<DamageInfo> onDamage;

        public float maxHealth = 100.0f;
        public float health = 100.0f;
        public float regenerateSpeed = 0.0f;
        public bool invincible = false;
        public bool useMaterial = false;

        protected Animator animator;

        private bool componentBased = false;
        private float lastDamageTime = 0f;
        
        private float damageUpdateSpeed = 10;

        private Material material;
        private Coroutine materialDamageCoroutine;

        private bool dead = false;
        public bool Dead { get { return dead; } set { dead = value; } }

        private void Start()
        {
            animator = GetComponent<Animator>();
            enabled = false;

            if (useMaterial)
            {
                material = GetComponentInChildren<Renderer>().material;
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers) r.sharedMaterial = material;
            }

            componentBased = GetComponentInChildren<ComponentHealth>() != null;
        }

        private void OnDestroy()
        {
            if (useMaterial && material != null)
                Destroy(material);
        }

        public void OnDamage(float amount) { OnDamage(new DamageInfo(amount)); }

        public virtual void OnDamage(DamageInfo damage)
        {
            if (invincible || dead || damage.amount <= 0 || componentBased)
                return;

            ApplyDamage(damage);
        }

        private void ApplyDamage(DamageInfo damage)
        {
            health -= damage.amount;
            lastDamageTime = Time.time;

            Vector3 hitDir = transform.InverseTransformDirection(damage.direction);

            ////will be re-enabled when animations will be fixed
            ////uncomment to have animations for damage
            //if (animator != null)
            //{
            //    animator.SetFloat("damageDirX", hitDir.x * .5f);
            //    animator.SetFloat("damageDirY", hitDir.z * .5f);
            //    animator.SetTrigger("damage");
            //}

            //if (materialDamageCoroutine != null)
            //    StopCoroutine(materialDamageCoroutine);

            //materialDamageCoroutine = StartCoroutine(DamageMaterialCoroutine(damage.amount));
            StartCoroutine(DamageMaterialCoroutine(damage.amount));

            if (regenerateSpeed > 0)
                enabled = true;

            if (health <= 0)
            {
                health = 0;
                dead = true;
                enabled = false;

                OnDeath();
            }

            onDamage?.Invoke(damage);
        }

        public virtual void OnComponentDamage(DamageInfo damage)
        {
            ApplyDamage(damage);
        }

        protected virtual void OnDeath() { onDeath?.Invoke(); }

        void OnEnable()
        {
            StartCoroutine(Regenerate());
            OnEnableComponent();
        }

        protected virtual void OnEnableComponent() { }

        IEnumerator Regenerate()
        {
            if (regenerateSpeed > 0.0f)
            {
                while (enabled)
                {
                    if (Time.time > lastDamageTime + 3)
                    {
                        health += regenerateSpeed;

                        yield return null;

                        if (health >= maxHealth)
                        {
                            health = maxHealth;
                            enabled = false;
                        }
                    }
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }

        public IEnumerator DamageMaterialCoroutine(float damage) 
        {
            if (!useMaterial) yield break;

            Color damageColor = material.GetColor("_DamageColor");
            while (damageColor.a < 1)
            {
                damageColor.a += damageUpdateSpeed * Time.deltaTime;
                material.SetColor("_DamageColor", damageColor);
                yield return null;
            }
            
            yield return null;

            while (damageColor.a > 0)
            {
                damageColor.a -= damageUpdateSpeed/2f * Time.deltaTime;
                material.SetColor("_DamageColor", damageColor);
                yield return null;
            }
        }
    }
}