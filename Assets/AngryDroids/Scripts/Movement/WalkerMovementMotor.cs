using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public class WalkerMovementMotor : FreeMovementMotor
    {
        //public AnimationCurve horizontalScaleCurve;
        public float horizontalAxisScale = 0.15f;
        public float verticalAxisScale = 0.33f;
        public float turnAxisScale = 1f;
        public float jumpForce;

        private bool grounded;

        void Update()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(rigidbody.velocity);
            //if (Mathf.Abs(rotationAngle) > stopOnTurningThreshold)
            //{
            //    animator.SetFloat("turn", rigidbody.angularVelocity.y * turnAxisScale);// rigidbody.angularVelocity.y * horizontalAxisScale);
            //    animator.SetFloat("vertical", 0f);
            //    animator.SetFloat("horizontal", 0f);
            //}
            //else
            //{
            //    animator.SetFloat("turn", 0f);
            //    animator.SetFloat("vertical", localVelocity.z * verticalAxisScale);
            //    animator.SetFloat("horizontal", localVelocity.x * horizontalAxisScale);
            //}
            
            //uncomment upper code in case turn/movement blending will work wrong
            //and comment/remove one below
            animator.SetFloat("turn", rigidbody.angularVelocity.y * turnAxisScale);
            animator.SetFloat("vertical", localVelocity.z * verticalAxisScale);
            animator.SetFloat("horizontal", localVelocity.x * horizontalAxisScale);
        }

        public void DoJump()
        {
            rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            grounded = false;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!grounded)
            {
                animator.SetTrigger("land");
                grounded = true;
            }
        }
    }
}