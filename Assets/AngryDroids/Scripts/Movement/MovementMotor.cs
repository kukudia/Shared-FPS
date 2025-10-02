using UnityEngine;
using System.Collections.Generic;

//TODO All droids navigate like this: moveVector is (horizontal, 0, vertical)
//NOW I want moveVector (horizontal(strafe), turn, vertical) 
//and for 3d navigation (horizontal or turn, world vertical, )
//but for AI horizontal is not used, while User is not using turn

namespace GravityBox.AngryDroids
{
    public class MovementMotor : MonoBehaviour, IMovementMotor
    {
        // The direction the character wants to move in, in world space.
        // The vector should have a length between 0 and 1.
        public Vector3 movementDirection { get; set; }

        // Simpler motors might want to drive movement based on a target purely
        public Vector3 movementTarget { get; set; }

        // The direction the character wants to face towards, in world space.
        public Vector3 facingDirection { get; set; }

        private Rigidbody _rigidbody;
        public new Rigidbody rigidbody 
        {
            get 
            {
                if (_rigidbody == null)
                    _rigidbody = GetComponent<Rigidbody>(); 
                return _rigidbody; 
            }
        }

        public static float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
        {
            // Project A and B onto the plane orthogonal target axis
            dirA = dirA - Vector3.Project(dirA, axis);
            dirB = dirB - Vector3.Project(dirB, axis);

            // Find (positive) angle between A and B
            float angle = Vector3.Angle(dirA, dirB);

            // Return angle multiplied with 1 or -1
            return angle * (Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) < 0 ? -1 : 1);
        }
    }

    public interface IMovementMotor
    {
        Vector3 movementDirection { get; set; }
        Vector3 movementTarget { get; set; }
        Vector3 facingDirection { get; set; }

        Rigidbody rigidbody { get; }
    }

    public static class ComponentExtentions
    {
        public static float GetBoundingSize(this Component component)
        {
            return component.GetComponent<Collider>().bounds.extents.magnitude;
        }
    }
}