using UnityEngine;
using System.Collections;
using GravityBox;

namespace GravityBox.AngryDroids
{
    public class LaserRayFX : ScriptedAnimation
    {
        public Gradient startColor;
        public Gradient endColor;
        public AnimationCurve lengthAnimation;
        public ParticleSystem laserTipParticle;
        public Light laserMainLight;

        public float length = 10f;
        public float width = 0.35f;

        public float damage;
        public float damageInterval;
        public GameObject hitPrefab;

        private float nextDamageTime = 0;
        private LineRenderer ren;
        private Material mat;
        private GameObject hitEffect;
        private IHealth lastHealth;
        private SmallObjectPool pool;

        public RaycastHit hit { get { return _hit; } set { hit = value; } }
        RaycastHit _hit;

        protected override void OnAwake()
        {
            pool = new SmallObjectPool(hitPrefab, 20);
        }

        protected override void OnEnableScript()
        {
            if (ren == null)
                ren = GetComponent<LineRenderer>();
            if (mat == null)
                mat = ren.material;

            laserTipParticle.gameObject.SetActive(true);

            //ren.SetColors(startColor.Evaluate(0), endColor.Evaluate(0));
            ren.startColor = startColor.Evaluate(0);
            ren.endColor = endColor.Evaluate(0);

            UpdateRay();

            lastHealth = null;
            nextDamageTime = Time.time;
        }

        protected override void OnDisableScript()
        {
            hitEffect = null;
            laserTipParticle.gameObject.SetActive(false);
        }

        protected override void OnDestroyGameObject()
        {
            Destroy(mat);
        }

        protected override void UpdateAnimation(float normalizedTime)
        {
            UpdateRay();
            ren.startColor = startColor.Evaluate(normalizedTime);
            ren.endColor = endColor.Evaluate(normalizedTime);
            ren.startWidth = width * lengthAnimation.Evaluate(normalizedTime);
            ren.endWidth = width * lengthAnimation.Evaluate(normalizedTime);
            //ren.SetColors(startColor.Evaluate(normalizedTime), endColor.Evaluate(normalizedTime));
            //ren.SetWidth(width * lengthAnimation.Evaluate(normalizedTime), width * lengthAnimation.Evaluate(normalizedTime));

            laserMainLight.color = startColor.Evaluate(normalizedTime);
        }

        void UpdateRay()
        {
            Vector3 endPosition;

            if (Physics.Raycast(transform.position, transform.forward, out _hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                endPosition = new Vector3(0, 0, _hit.distance);

                if (_hit.collider != null)
                    HandleDamage(_hit.collider);

                if (hitEffect == null)
                {
                    //hitEffect = Instantiate(hitPrefab, _hit.point, Quaternion.LookRotation(_hit.normal, Vector3.up)) as GameObject;
                    hitEffect = pool.Get();
                    hitEffect.SetActive(true);
                }
                //else
                //{
                    hitEffect.transform.position = _hit.point;
                    hitEffect.transform.rotation = Quaternion.LookRotation(_hit.normal, Vector3.up);
                //}

                //hitEffect.transform.parent = _hit.transform;
            }
            else
            {
                endPosition = new Vector3(0, 0, 100f);
                hitEffect = null;
            }

            ren.SetPosition(1, endPosition);
            laserTipParticle.transform.localPosition = endPosition;
        }

        void HandleDamage(Collider body)
        {
            lastHealth = body.GetComponent(typeof(IHealth)) as IHealth;
            if (lastHealth != null && Time.time > nextDamageTime)
            {
                lastHealth.OnDamage(new DamageInfo(DamageType.Laser, damage, transform.forward));
                nextDamageTime = Time.time + damageInterval;
            }
        }
    }
}
