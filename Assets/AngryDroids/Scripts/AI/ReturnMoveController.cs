using UnityEngine;

namespace GravityBox.AngryDroids
{
    /// <summary>
    /// Return to initial position or back to patroling routine
    /// There is no need to separate this two behaviours, 
    /// otherwise we would have to make too droid prefab variants for every behaviours
    /// </summary>
    public class ReturnMoveController : AIBehaviour
    {
        public PatrolRoute patrolRoute;
        
        private delegate void UpdateAction();
        private UpdateAction onUpdate;

        private const float patrolPointRadius = 0.5f;
        private int nextPatrolPoint = 0;
        private int patrolDirection = 1;
        
        void OnEnable() 
        {
            if (patrolRoute != null)
                onUpdate = NavigationPatrolSimple;
            else
                onUpdate = NavigationSimple;
        }

        private void OnDisable()
        {
            onUpdate = null;
        }

        void Update()
        {
            onUpdate();
        }

        void NavigationSimple() 
        {
            Vector3 movementDirection = ai.spawnPos - character.position;
            movementDirection.y = 0;
            motor.facingDirection = movementDirection;

            //if not there yet keep moving
            //otherwise disable itself
            if (movementDirection.sqrMagnitude > 0.1f)
            {
                motor.movementDirection = movementDirection.normalized;
            }
            else
            {
                motor.movementDirection = Vector3.zero;
                ai.Activate(false);
                enabled = false;
            }
        }

        void NavigationPatrolSimple() 
        {
            //activate Ai in case it's disabled on game start for example
            if (!ai.IsActive())
                ai.Activate(true);

            //wait until droid gets out
            if (!ai.IsReady())
            {
                motor.movementDirection = Vector3.zero;
                return;
            }

            // Early out if there are no patrol points
            if (patrolRoute == null || patrolRoute.points.Count == 0)
                return;

            // Find the vector towards the next patrol point.
            Vector3 targetVector = patrolRoute.GetPoint(nextPatrolPoint) - character.position;
            targetVector.y = 0;

            // If the patrol point has been reached, select the next one.
            if (targetVector.sqrMagnitude < patrolPointRadius * patrolPointRadius)
            {
                nextPatrolPoint += patrolDirection;
                if (nextPatrolPoint < 0)
                {
                    nextPatrolPoint = 1;
                    patrolDirection = 1;
                }
                if (nextPatrolPoint >= patrolRoute.points.Count)
                {
                    if (patrolRoute.pingPong)
                    {
                        patrolDirection = -1;
                        nextPatrolPoint = patrolRoute.points.Count - 2;
                    }
                    else
                    {
                        nextPatrolPoint = 0;
                    }
                }
            }

            // Make sure the target vector doesn't exceed a length if one
            if (targetVector.sqrMagnitude > 1)
                targetVector.Normalize();

            // Set the movement direction.
            motor.movementDirection = targetVector;
            // Set the facing direction.
            motor.facingDirection = targetVector;
        }
    }
}