using UnityEngine;
using UnityEngine.AI;

namespace GravityBox.AngryDroids
{
    public class NavMeshMovementMotor : MonoBehaviour, IMovementMotor
    {
        public bool controlRotation;
        public bool stopWhenTurning;
        public bool lookWhereGoing = true;

        [SerializeField]
        private float angleStopThreshold = 1.5f;
        [SerializeField]
        private float angleStartThreshold = 10f;

        private NavMeshAgent _agent;
        private Rigidbody _rigidbody;

        private float _angleToTurn;
        private bool _isRotating = false;
        private Vector3 _facingDirection;

        public Vector3 movementDirection
        {
            get { return agent.steeringTarget; }
            set
            {
                if (value.sqrMagnitude <= float.Epsilon) return;
                agent.SetDestination(transform.position + value);
            }
        }

        public Vector3 movementTarget
        {
            get { return agent.destination; }
            set { agent.SetDestination(value); }
        }

        public Vector3 facingDirection
        {
            get { return transform.forward; }
            set { _facingDirection = value; }
        }

        public Vector3 velocity => agent.velocity;
        public bool isTurning => _isRotating;
        public float turningAngle => _angleToTurn;

        public new Rigidbody rigidbody
        {
            get
            {
                if (_rigidbody == null)
                    _rigidbody = GetComponent<Rigidbody>();
                return _rigidbody;
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

        private void Awake()
        {
            agent.updateRotation = !controlRotation;
        }

        private void FixedUpdate()
        {
            if (controlRotation)
            {
                Vector3 steering = (agent.steeringTarget - transform.position);
                if (lookWhereGoing && steering.sqrMagnitude > 1f)
                {
                    steering.y = 0f;
                    steering.Normalize();
                    _angleToTurn = Vector3.SignedAngle(transform.forward, steering, transform.up);
                }
                else if (_facingDirection != Vector3.zero)
                {
                    _facingDirection.y = 0f;
                    _facingDirection.Normalize();
                    _angleToTurn = Vector3.SignedAngle(transform.forward, _facingDirection, transform.up);
                }
                else
                {
                    _angleToTurn = 0;
                }

                float angleValue = Mathf.Abs(_angleToTurn);
                if (stopWhenTurning && (angleValue > angleStartThreshold && !_isRotating))
                    StopAndTurn();

                if (_isRotating && angleValue < angleStopThreshold)
                    ResumeAfterTurn();

                UpdateRotation();

                //making character full stoll stop here because of rigidbody shaking
                if (agent.pathStatus == NavMeshPathStatus.PathComplete)
                {
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                }
            }
        }

        private void StopAndTurn()
        {
            agent.isStopped = true;
            _isRotating = true;
        }

        private void ResumeAfterTurn()
        {
            _isRotating = false;
            agent.isStopped = false;
        }

        private void UpdateRotation()
        {
            Vector3 angularVelocity = Vector3.zero;
            float frameAngleVelocity = agent.angularSpeed * Time.fixedDeltaTime;

            if (frameAngleVelocity > Mathf.Abs(_angleToTurn))
                angularVelocity.y = _angleToTurn;
            else
                angularVelocity.y = agent.angularSpeed * Mathf.Sign(_angleToTurn) * Time.fixedDeltaTime;

            Quaternion deltaRotation = Quaternion.Euler(angularVelocity);
            rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
        }
    }
}