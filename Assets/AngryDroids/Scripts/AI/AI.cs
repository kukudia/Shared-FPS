using UnityEngine;
using System.Collections.Generic;

namespace GravityBox.AngryDroids
{

    public class AI : MonoBehaviour
    {
        public AIBehaviour behaviourOnSpotted;        
        public AIBehaviour behaviourOnLostTrack;

        [SerializeField]
        private AIActivator activator;
        private AISight _sight;
        private Transform _character;
        private Animator _animator;
        private Transform _player;
        private Vector3 _spawnPos;
        private IMovementMotor _motor;
        private IWeapon _weapon;
        private IHealth _health;

        private static HashSet<AI> activeAIs = new HashSet<AI>();
        
        //player(or target) components;
        private IHealth targetHealth;
        private Collider targetCollider;

        //players can be of two kinds Rigidbody or CharacterController driven
        private Rigidbody targetRigidbody;
        private CharacterController targetController;

        //will be used later for AI debugging
        protected string _status;
        public string status => _status;

        public AISight sight => _sight;
        public Transform character => _character;
        public Animator animator => _animator;
        public Transform player => _player;
        public Vector3 spawnPos => _spawnPos;
        public IMovementMotor motor => _motor;
        public IWeapon weapon => _weapon;
        public IHealth health => _health;

        public static ICollection<AI> AIs { get { return activeAIs; } }

        void Awake()
        {
            if (_character == null)
            {
                _character = transform.parent;
                _spawnPos = transform.position;
                _animator = GetComponentInParent<Animator>();
                _sight = GetComponentInParent<AISight>();
                _motor = character.GetComponent<IMovementMotor>();                
                _weapon = character.GetComponent<IWeapon>();
                _health = character.GetComponent<IHealth>();
            }
        }

        void Start()
        {
            behaviourOnLostTrack.enabled = true;
            behaviourOnSpotted.enabled = false;

            activator.gameObject.SetActive(true);
        }

        void OnEnable() 
        {
            if (character.gameObject.activeInHierarchy)
                activeAIs.Add(this);
        }

        void OnDisable()
        {
            behaviourOnLostTrack.enabled = false;
            behaviourOnSpotted.enabled = false;

            if (!character.gameObject.activeInHierarchy)
                activeAIs.Remove(this);

            SetTarget(null);
        }

        HashSet<Collider> enemies = new HashSet<Collider>();
        private void OnTriggerEnter(Collider other)
        {
            //exit early if droid is attacking someone
            if (behaviourOnSpotted.enabled) return;

            CheckIfEnemy(other);
            //enemies.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            enemies.Remove(other);
        }

        private void FixedUpdate()
        {
            //exit early if droid is attacking someone
            if (behaviourOnSpotted.enabled) return;

            //pick first available enemy (single one in most cases)
            foreach (var enemy in enemies) 
            {
                if(CheckIfEnemy(enemy))
                    return;
            }
        }

        bool CheckIfEnemy(Collider other) 
        {
            ////ignore triggers as most of activators in scene are colliders (but also triggerts)
            //if (other.isTrigger) return;
            ////if target has different faction, i.e. different tag
            //if (other.tag == character.tag) return;
            ////if it is a moving and a living(can be killed) creature
            //if (other.attachedRigidbody == null && other.GetComponentInChildren(typeof(IHealth)) == null) return;
            ////if ai might see it (shoot it) from where it is, but is not looking at it directly
            if (IsEnemy(other))
            {
                enemies.Add(other);
                if (sight.CanSeeTargetFromPoint(other))
                {
                    //Debug.DrawLine(sight.sight.position, other.bounds.center);
                    SetTarget(other.transform);
                    OnSpotted();
                    return true;
                }
            }

            return false;
        }

        bool IsEnemy(Collider other) 
        {
            if (other == null) return false;
            //ignore triggers as most of activators in scene are colliders (but also triggerts)
            if (other.isTrigger) return false;
            //if target has different faction, i.e. different tag
            if (other.tag == character.tag) return false;
            //if it is a moving and a living(can be killed) creature
            if (other.attachedRigidbody == null && other.GetComponentInChildren(typeof(IHealth)) == null) return false;

            return true;
        }

        public void SetTarget(Transform target)
        {
            _player = target;
            _sight.player = target;

            targetCollider = target != null ? _player.GetComponent<Collider>() : null;
            targetController = target != null ? _player.GetComponent<CharacterController>() : null;
            targetRigidbody = target != null ? _player.GetComponent<Rigidbody>() : null;
            targetHealth = target != null ? _player.GetComponent<IHealth>() : null;
        }

        public void OnSpotted()
        {
            if (_player == null) return;

            if (!IsActive())
                Activate(true);

            if (!behaviourOnSpotted.enabled)
            {
                behaviourOnSpotted.enabled = true;
                behaviourOnLostTrack.enabled = false;
            }
        }

        public void OnLostTrack()
        {
            if (CanSeePlayer()) return;

            if (!behaviourOnLostTrack.enabled)
            {
                behaviourOnLostTrack.enabled = true;
                behaviourOnSpotted.enabled = false;
            }

            SetTarget(null);
        }

        public bool CanSeePlayer()
        {
            if (targetCollider == null) 
                return false;
            else
                return sight.CanSee(targetCollider);
        }

        public bool IsPlayerDead()
        {
            if(_player == null) 
                return true; 
            else 
                return (targetHealth == null || targetHealth.Dead);
        }

        public Vector3 GetPlayerDirection(bool horizontal = false) { return horizontal ? sight.GetTargetGroundDirection(_player) : sight.GetTargetDirection(targetCollider); }
        public Vector3 GetPlayerNearestDirection(float offsetFromPlayer) { return sight.GetPlayerNearestDirection(offsetFromPlayer); }
        public Vector3 GetPlayerVelocity() 
        {
            if(_player == null) 
                return Vector3.zero; 
            else  
                return targetRigidbody!=null ? targetRigidbody.velocity : targetController.velocity;   
        }

        public void Activate(bool active) { _animator.SetBool("active", active); _weapon.Activate(active); }
        public bool IsActive() { return _animator.GetBool("active"); }
        public bool IsReady() { return _animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Active"); }
        public bool IsOffline() { return _animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Inactive"); }
        public bool IsAttacking() { return behaviourOnSpotted.enabled; }
        public bool IsReturning() { return behaviourOnLostTrack.enabled; }
    }
}