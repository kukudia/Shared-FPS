using UnityEngine;

namespace GravityBox.AngryDroids
{
    [RequireComponent (typeof(Rigidbody))]
    public class FreeMovementMotor : MovementMotor 
    {
	    public float walkingSpeed = 5.0f;
	    public float walkingSnappyness = 50f;
	    public float turningSmoothing = 0.3f;
        public bool stopOnTurning = false;
        public float stopOnTurningThreshold = 9;

        protected float rotationAngle = 0;

        private Animator _animator;
        public Animator animator
        {
            get { if(_animator == null) _animator = GetComponent<Animator>(); return _animator; }
            set { _animator = value; }
        }

        void OnDisable() 
        {
            rigidbody.velocity = Vector3.zero; 
            rigidbody.angularVelocity = Vector3.zero; 
        }

        void FixedUpdate()
        {
            // Handle the movement of the character
            Vector3 targetVelocity = movementDirection * walkingSpeed;
            Vector3 deltaVelocity = targetVelocity - rigidbody.velocity;
            if (rigidbody.useGravity)
                deltaVelocity.y = 0;

            // Setup player to face facingDirection, or if that is zero, then the movementDirection
            Vector3 faceDir = facingDirection;
            if (faceDir == Vector3.zero)
                faceDir = movementDirection;

            // Make the character rotate towards the target rotation
            if (faceDir == Vector3.zero)
            {
                rigidbody.angularVelocity = Vector3.zero;
                rotationAngle = 0f;
            }
		    else 
            {
		        rotationAngle = AngleAroundAxis (transform.forward, faceDir, Vector3.up);
			    rigidbody.angularVelocity = (Vector3.up * rotationAngle * turningSmoothing);
            }

            if (!stopOnTurning || Mathf.Abs(rotationAngle) < stopOnTurningThreshold)
                rigidbody.AddForce(deltaVelocity * walkingSnappyness, ForceMode.Acceleration);
        }	
    }
}