using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public class TeslaRayFX : ScriptedAnimation
    {
        public class LightRay
        {
            public float length = 10f;
            private TeslaRayFX controller;
            private LineRenderer renderer;
            private Material material;

            private float creationTime = -1;
            const float rebuildInterval = 0.075f;

            private Vector2 baseTextureScale;
            private Vector2 baseTextureOffset;

            public LightRay(TeslaRayFX controller)
            {
                this.controller = controller;
                GameObject clone = new GameObject("ray");

                material = Instantiate(controller.material);

                renderer = clone.AddComponent<LineRenderer>();
                renderer.sharedMaterial = material;
                renderer.positionCount = RAY_SUBDIV + 1;
                //renderer.SetVertexCount(TeslaRayFX.RAY_SUBDIV + 1);
                //renderer.SetWidth(0.25f, 0.35f);
                renderer.startWidth = 0.25f;
                renderer.endWidth = 0.35f;
                renderer.useWorldSpace = true;

                clone.transform.parent = controller.transform.parent;
                clone.transform.localPosition = controller.transform.localPosition;
                clone.transform.localRotation = controller.transform.localRotation;
                
                baseTextureScale = material.GetTextureScale("_MainTex");
                baseTextureOffset = material.GetTextureScale("_MainTex");
            }

            public void Reset()
            {
                creationTime = Time.time;
                
                //renderer.SetColors(controller.lineStartColor.Evaluate(0), controller.lineEndColor.Evaluate(0));
                renderer.startColor = controller.lineStartColor.Evaluate(0);
                renderer.endColor = controller.lineEndColor.Evaluate(0);
                material.SetTextureScale("_MainTex", Vector3.Scale(baseTextureScale, new Vector2(length / 10f, 1f)));
                material.SetTextureOffset("_MainTex", new Vector3(Random.Range(0f, 1f), baseTextureOffset.y));
                
                Rebuild();
            }

            public void Update(float time)
            {
                if (creationTime + rebuildInterval < Time.time)
                    Rebuild();

                float current = time;
                //Color endColor = controller.lineEndColor.Evaluate(current);
                
                //renderer.SetColors(controller.lineStartColor.Evaluate(current), endColor);
                renderer.startColor = controller.lineStartColor.Evaluate(current);
                renderer.endColor = controller.lineEndColor.Evaluate(current);
                renderer.SetPosition(0, controller.transform.position);
                renderer.SetPosition(TeslaRayFX.RAY_SUBDIV, controller.hitPoint);

                material.SetTextureOffset("_MainTex", new Vector3(controller.lineOffset.Evaluate(current), 0.0f));
            }

            void Rebuild()
            {
                creationTime = Time.time;
                Vector3 start = controller.transform.position;
                Vector3 end = controller.hitPoint;
                Vector3 point;
                //renderer.SetVertexCount(TeslaRayFX.RAY_SUBDIV + 1);
                renderer.positionCount = RAY_SUBDIV + 1;
                renderer.SetPosition(0, start);
                int count = TeslaRayFX.RAY_SUBDIV;
                for (int i = 1; i < count; i++)
                {
                    point = Vector3.Lerp(start, end, i / (float)count);
                    point += Random.insideUnitSphere * controller.lineDeviation;
                    renderer.SetPosition(i, point);
                }
                renderer.SetPosition(count, end);
            }

            public void Dispose()
            {
                Destroy(material);
                Destroy(renderer.gameObject);
            }
        }

        [System.NonSerialized]
        public Transform target;
        public LayerMask layerMask;

        const int RAY_COUNT = 2;
        const int RAY_SUBDIV = 3;
        const float FAKE_RAY_SIZE = 20f;

        public LightRay[] lines;

        public Material material;
        public Gradient lineStartColor;
        public Gradient lineEndColor;
        public AnimationCurve lineOffset;
        public float lineDeviation = 0.75f;

        public float damage;
        public float damageInterval;
        public GameObject hitPrefab;

        float nextDamageTime = 0;
        Collider targetCollider;
        IHealth lastHealth;
        GameObject hitEffect;

        Vector3 hitPoint 
        {
            get 
            {
                if (target)
                    lastHitPoint = target.TransformPoint(targetHitPoint);
                else
                    lastHitPoint = transform.position + transform.forward * FAKE_RAY_SIZE;

                return lastHitPoint;
            }
        }
        Vector3 targetHitPoint;
        Vector3 lastHitPoint;

        public RaycastHit hit { get { return _hit; } set { hit = value; } }
        RaycastHit _hit;
        
        protected override void OnEnableScript()
        {
            Vector3 forward;
            //Debug.Break();
            //if (target != null)
            //    targetCollider = target.GetComponent<Collider>();

            //if (targetCollider != null)
            //    forward = targetCollider.bounds.center - transform.position;
            //else
                forward = transform.forward;
            
            if (lines == null || lines.Length == 0)
            {
                lines = new LightRay[RAY_COUNT];
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = new LightRay(this);
                }
            }

            if (Physics.Raycast(transform.position, forward, out _hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                targetCollider = _hit.collider;
                target = _hit.transform;
                targetHitPoint = target.InverseTransformPoint(_hit.point);

                if (hitEffect == null)
                {
                    hitEffect = Instantiate(hitPrefab, _hit.point, Quaternion.LookRotation(_hit.normal, Vector3.up)) as GameObject;
                }
                else
                {
                    hitEffect.transform.position = _hit.point;
                    hitEffect.transform.rotation = Quaternion.LookRotation(_hit.normal, Vector3.up);
                }
            }
            else
            {
                targetHitPoint = transform.position + forward * FAKE_RAY_SIZE;
            }

            foreach (LightRay l in lines)
            {
                l.length = targetCollider != null ? hit.distance : FAKE_RAY_SIZE;
                l.Reset();
            }
        }
        
        protected override void OnDisableScript()
        {
            hitEffect = null;
            target = null;
            targetCollider = null;
        }

        protected override void OnDestroyGameObject()
        {
            foreach (LightRay l in lines)
                l.Dispose();
        }
       
        protected override void UpdateAnimation(float normalizedTime)
        {
            if (targetCollider != null)
                HandleDamage(targetCollider);

            foreach (LightRay r in lines)
                r.Update(normalizedTime);
        }

        void HandleDamage(Collider body)
        {
            lastHealth = body.GetComponent(typeof(IHealth)) as IHealth;
            if (lastHealth != null && Time.time > nextDamageTime)
            {
                lastHealth.OnDamage(new DamageInfo(DamageType.Electric, damage, transform.forward));
                nextDamageTime = Time.time + damageInterval;
            }
        }
    }
}
