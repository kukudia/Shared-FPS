using UnityEngine;

namespace GravityBox.AngryDroids
{
    public class ADWalkerAnimator : MonoBehaviour
    {
        private Animator _animator;
        private NavMeshMovementMotor _motor;

        public Animator animator
        {
            get
            {
                if (_animator == null)
                    _animator = GetComponent<Animator>();
                return _animator;
            }
        }

        public NavMeshMovementMotor motor
        {
            get
            {
                if (_motor == null)
                    _motor = GetComponent<NavMeshMovementMotor>();
                return _motor;
            }
        }

        void Update()
        {
            Vector3 current = motor.velocity;
            current = transform.InverseTransformDirection(current);
            animator.SetFloat("vertical", motor.isTurning ? 0 : current.z);
            animator.SetFloat("horizontal", motor.isTurning ? 0 : current.x);
            animator.SetFloat("turn", motor.isTurning ? -motor.turningAngle / 15f : 0);
        }
    }
}