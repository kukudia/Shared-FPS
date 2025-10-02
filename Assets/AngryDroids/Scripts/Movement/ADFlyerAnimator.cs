using UnityEngine;
using UnityEngine.AI;

namespace GravityBox.AngryDroids
{
    public class ADFlyerAnimator : MonoBehaviour
    {
        public float flyHeight = 2f;
        public float liftOffSpeed = 5f;
        private NavMeshAgent _agent;
        private Animator _animator;

        public Animator animator
        {
            get
            {
                if (_animator == null)
                    _animator = GetComponent<Animator>();
                return _animator;
            }
        }

        public NavMeshAgent agent
        {
            get
            {
                if (_agent == null)
                    _agent = GetComponent<NavMeshAgent>();
                return _agent;
            }
        }

        private void Update()
        {
            agent.baseOffset = Mathf.Lerp(agent.baseOffset, IsActive() ? flyHeight : 0, liftOffSpeed * Time.deltaTime);
        }

        private bool IsActive() { return animator.GetBool("active"); }
    }
}