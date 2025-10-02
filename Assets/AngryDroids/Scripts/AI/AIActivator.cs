using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public class AIActivator : MonoBehaviour
    {
        GameObject targetCharacter;
        SphereCollider sphereCollider;
        float activeRadius;

        void OnEnable()
        {
            if (targetCharacter == null)
            {
                targetCharacter = transform.parent.gameObject;
                sphereCollider = GetComponent<SphereCollider>();
                activeRadius = sphereCollider.radius;
            }

            bool enemyInside = false;
            Collider[] colliders = Physics.OverlapSphere(sphereCollider.center, sphereCollider.radius);
            foreach (Collider c in colliders)
            {
                if (c.tag != targetCharacter.tag)
                    enemyInside = true;
            }

            if(!enemyInside)
                Deactivate();
        }

        void OnDisable() { }

        void OnTriggerEnter(Collider other)
        {
            if (IsEnemy(other) && targetCharacter.transform.parent == transform)
            {
                Activate();
            }
        }

        //should this be triggered at all?
        //or should this become disabled when out of camera? 
        void OnTriggerExit(Collider other)
        {
            if (IsEnemy(other))
            {
                Deactivate();
            }
        }

        public void Activate()
        {
            //attach AI to level if attached
            targetCharacter.transform.parent = transform.parent;
            targetCharacter.SetActive(true);
            //attach activator to AI
            transform.parent = targetCharacter.transform;
            sphereCollider.radius = activeRadius * 1.1f;
        }

        public void Deactivate()
        {
            //attach activator to level
            transform.parent = targetCharacter.transform.parent;
            //make activator AI's parent
            targetCharacter.transform.parent = transform;
            targetCharacter.SetActive(false);
            sphereCollider.radius = activeRadius;
        }

        bool IsEnemy(Collider other) 
        {
            //check if tag is different and this is a living thing
            return (other.tag != targetCharacter.tag && other.GetComponentInChildren<IHealth>() != null);
        }
    }
}