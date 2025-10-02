using UnityEngine;

namespace GravityBox.AngryDroids
{
    [RequireComponent(typeof(Rigidbody))]
    public class HoverMovementMotor : MovementMotor 
    {
        public float hoverHeight = 1.2f;
	    public float flyingSpeed = 5.0f;
	    public float flyingSnappyness = 2.0f;
        public bool turn = true;
        public float turningSpeed = 3.0f;
	    public float turningSnappyness = 3.0f;
        public float bankingAmount = 1.0f;

        void FixedUpdate () 
        {
		    // Handle the movement of the character
            Vector3 targetVelocity = movementDirection * flyingSpeed;
            
            if (Physics.Raycast(transform.position, Vector3.down, hoverHeight) && targetVelocity.magnitude > 0.1f)
                targetVelocity += Vector3.up * Time.deltaTime * flyingSpeed;

            Vector3 deltaVelocity = targetVelocity - rigidbody.velocity;
		    rigidbody.AddForce (deltaVelocity * flyingSnappyness, ForceMode.Acceleration);

            if (turn)
            {
                Vector3 faceDir = facingDirection;
                if (faceDir == Vector3.zero)
                    faceDir = movementDirection;

                // Make the character rotate towards the target rotation
                if (faceDir == Vector3.zero)
                    rigidbody.angularVelocity = Vector3.zero;
                else
                {
                    float rotationAngle = AngleAroundAxis(transform.forward, faceDir, Vector3.up);
                    rigidbody.angularVelocity = (Vector3.up * rotationAngle * turningSnappyness);
                }
                /*
                // Make the character rotate towards the target rotation
                Vector3 facingDir = facingDirection != Vector3.zero ? facingDirection : movementDirection;
                if (facingDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(facingDir, Vector3.up);
                    Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.rotation);
                    Vector3 axis;
                    float angle;
                    deltaRotation.ToAngleAxis(out angle, out axis);
                    Vector3 deltaAngularVelocity = axis * Mathf.Clamp(angle, -turningSpeed, turningSpeed) - rigidbody.angularVelocity;

                    float banking = Vector3.Dot(movementDirection, -transform.right);

                    rigidbody.AddTorque(deltaAngularVelocity * turningSnappyness + transform.forward * banking * bankingAmount);
                }
                */
            }
	    }

        void OnCollisionStay (Collision collisionInfo) 
        {
            //if (!ai.IsReady()) return;
            if (movementDirection.sqrMagnitude < 0.01f) return;
		    // Move up if colliding with static geometry
		    if (collisionInfo.rigidbody == null)
			    rigidbody.velocity += Vector3.up * Time.deltaTime * 50;
	    }	
    }
}