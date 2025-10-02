using UnityEngine;

namespace GravityBox.AngryDroids
{
    /// <summary>
    /// Simplest behaviour best for melee attack droid
    /// for more complex weapons might be too primitive to use
    /// </summary>
    public class FlyerAttackMoveController : AIBehaviour
    {
        public float enemyRangeMin = 0.3f;

        private Vector3 lastPlayerPosition = Vector3.zero;
        private float lostPlayerTime = -1f;
        private float noticePlayerTime = -0.7f;

        void Update()
        {
            if (!ai.IsReady())
            {
                //do nothing until ai is fully functional
                motor.movementDirection = Vector3.zero;
                return;
            }

            Move();

            //if player was killed
            if (ai.player != null && ai.IsPlayerDead())
                ai.OnLostTrack();
        }

        private void Move()
        {
            if (Time.time < noticePlayerTime + 0.8f)
            {
                motor.movementDirection = Vector3.zero;
                return;
            }

            if (ai.CanSeePlayer())
            {
                Vector3 playerDirection = ai.GetPlayerNearestDirection(enemyRangeMin);//(0.5f);

                //slow down when close to player
                if (playerDirection.magnitude < enemyRangeMin)
                    motor.movementDirection *= 0.1f;
                else
                    motor.movementDirection = playerDirection.normalized;

                //this is melee attack so no seek and no checks
                weapon.Fire(player);

                lostPlayerTime = -1f;
                lastPlayerPosition = ai.player.position;
            }
            else
            {
                if (lostPlayerTime < 0)
                    lostPlayerTime = Time.time;

                Vector3 lastPlayerDirection = (lastPlayerPosition - ai.character.position).normalized;
                motor.movementDirection = lastPlayerDirection;
                motor.facingDirection = lastPlayerDirection;

                if (lostPlayerTime > 0 && Time.time > lostPlayerTime + 3f)
                {
                    lostPlayerTime = -1f;
                    lastPlayerPosition = Vector3.zero;
                    ai.OnLostTrack();
                }
            }
        }
    }
}