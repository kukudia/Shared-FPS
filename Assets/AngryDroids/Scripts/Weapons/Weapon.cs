using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public interface IWeaponSimple : IUnityObject
    {
        bool CanFire();
        void Fire();
    }

    public class Weapon : MonoBehaviour, IWeaponSimple
    {
        public float cooldown = 1f;
        public Transform weaponTransform;

        private float _lastFireTime = 0f;
        [SerializeField]
        private Animator _animator = null;

        public bool isFiring
        {
            get { return _isFiring; }
            set { _isFiring = value; }
        }
        private bool _isFiring = false;

        void Awake()
        {
            if (weaponTransform == null)
                weaponTransform = transform;

            _lastFireTime = -cooldown;
            OnAwake();
        }

        protected virtual void OnAwake() { }

        public virtual bool CanFire()
        {
            return !_isFiring && !IsCoolingDown();
        }

        public bool IsCoolingDown()
        {
            return (_lastFireTime + cooldown > Time.time);
        }

        public void Fire()
        {
            if (!CanFire()) return;

            _isFiring = true;
            _lastFireTime = Time.time;

            if (_animator != null)
                _animator.SetTrigger("fire");

            StartCoroutine(FireCoroutine());
        }

        protected virtual IEnumerator FireCoroutine()
        {
            yield return null;
            _isFiring = false;
        }
    }
}