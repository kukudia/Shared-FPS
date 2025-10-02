using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public interface IUnityObject
    {
        GameObject gameObject { get; }
        Transform transform { get; }
    }

    public interface IWeapon : IUnityObject
    {
        float fov { get; }
        float rangeMin { get; }
        float rangeMax { get; }
        bool isFiring { get; set; }

        bool CanFire();
        bool IsInRange(Transform target);
        void Fire();
        void Fire(Transform target);
        void Seek(Transform target);
        void Activate(bool active);
    }

    public class WeaponAI : MonoBehaviour, IWeapon
    {
        public Transform weaponTransform;

        public float cooldown = 2f;

        public WeaponSeek targeting;

        [SerializeField]
        private float _rangeMax = 1000f;
        [SerializeField]
        private float _rangeMin = 1f;
        [SerializeField]
        private float _fov = 180;
        
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private Animator _owner;

        private bool _isFiring;
        private float _lastFireTime;
        private Collider _seekTarget;

        public float rangeMax { get { return _rangeMax; } }
        public float rangeMin { get { return _rangeMin; } }
        public float fov { get { return _fov; } }
        public bool isFiring { get { return _isFiring; } set { _isFiring = value; } }

        void Awake()
        {            
            if (weaponTransform == null)
                weaponTransform = transform;

            targeting.Awake();
            
            _lastFireTime = -cooldown;
            OnAwake();
        }

        protected virtual void OnAwake() { }

        public virtual bool IsInRange(Transform target)
        {
            return Sight.IsVisible(target.GetComponent<Collider>().bounds, weaponTransform, fov, rangeMin, rangeMax);
        }

        public virtual bool CanFire()
        {
            return !_isFiring && !IsCoolingDown();
        }

        public bool IsCoolingDown() 
        {
            return (_lastFireTime + cooldown > Time.time);
        }

        public void Activate(bool active) 
        {
            if(_animator != null)
                _animator.SetBool("active", active); 
        }

        public virtual void Fire()
        {
            if (!CanFire()) return;

            _isFiring = true;
            _lastFireTime = Time.time;

            if (_owner != null)
                _owner.SetTrigger("fire");

            if (_animator!=null)
                _animator.SetTrigger("fire");

            StartCoroutine(FireCoroutine());
        }

        public virtual void Fire(Transform target) 
        {
            if (IsInRange(target))
                Fire();
        }

        public virtual void Seek(Transform target)
        {
            //assign object for targeting to track
            _seekTarget = target.GetComponent<Collider>();
        }

        protected virtual IEnumerator FireCoroutine() 
        {
            yield return null;
            _isFiring = false;
        }

        private void LateUpdate()
        {
            //here all targeting is happening to avoid animation
            //affecting head or weapon rotation
            if(_seekTarget != null)
            {
                targeting.Seek(_seekTarget.bounds.center);
                _seekTarget = null;
            }
        }
    }
}