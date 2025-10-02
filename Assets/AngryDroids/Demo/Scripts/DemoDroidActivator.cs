using UnityEngine;

namespace GravityBox.AngryDroids.Demo
{
    /// <summary>
    /// Droid starter, activate and auto-deactivate droid with this component
    /// That can probably be used on any droid
    /// </summary>
    public class DemoDroidActivator : MonoBehaviour
    {
        public bool autoDeactivate = true;

        protected IWeapon m_Weapon;
        protected Animator m_Animator;

        private float deactivationTimer = 5f;

        public bool isActive { get { return m_Animator.GetBool("active"); } set { m_Animator.SetBool("active", value); } }
        public void Activate(bool active) { m_Animator.SetBool("active", active); }
        public bool isReady { get { return m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Active"); } }
        public bool IsOffline() { return m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Inactive"); }
        public bool IsMoving() { return m_Animator.GetFloat("vertical") != 0f || m_Animator.GetFloat("turn") != 0f; }

        void Awake()
        {
            m_Weapon = GetComponent<IWeapon>();
            m_Animator = GetComponent<Animator>();
        }

        public void Update()
        {
            //activator
            if (IsMoving())
            {
                if (!isActive)
                    isActive = true;
            }

            //deactivator
            if (autoDeactivate)
            {
                if (IsMoving() || m_Weapon.isFiring)
                    deactivationTimer = 5f;
                else
                    deactivationTimer -= Time.deltaTime;

                if (deactivationTimer <= 0f)
                {
                    isActive = false;
                    deactivationTimer = 5f;
                }
            }
        }
    }
}