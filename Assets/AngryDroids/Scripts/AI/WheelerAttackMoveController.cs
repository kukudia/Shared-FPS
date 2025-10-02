using UnityEngine;

namespace GravityBox.AngryDroids
{
    /// <summary>
    /// AI behaviour for droids on wheels
    /// I already see a similarities with walkers and hovers
    /// Should be merged into one (probably)
    /// </summary>
    public class WheelerAttackMoveController : AIBehaviour
    {
        //public float targetDistanceFront = 2.5f;
        //public float targetDistanceBack = 1.0f;

        public float enemyRangeMin = 2.0f;
        public float enemyRangeMax = 3.0f;

        private bool inRange = false;
        private float noticePlayerTime = -0.7f;
        private float lostPlayerTime = -1f;
        private Vector3 lastPlayerPosition = Vector3.zero;

        void OnEnable() 
        {
            inRange = false;
            lostPlayerTime = -1f;
            noticePlayerTime = Time.time;

            if (!ai.IsActive())
                ai.Activate(true);
        }

        void Update()
        {
            if (!ai.IsReady())
            {
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
            // For a short moment after noticing player,
            // only look at him but don't walk towards or attack yet.
            if (Time.time < noticePlayerTime + 0.8f)
            {
                motor.movementDirection = Vector3.zero;
                return;
            }

            Vector3 playerDirection = ai.GetPlayerDirection(true);
            Vector3 playerVelocity = ai.GetPlayerVelocity();

            float playerDist = playerDirection.magnitude;
            playerDirection /= playerDist;

            //if (inRange && playerDist > enemyRangeMax)
            //    inRange = false;
            //if (!inRange && playerDist < enemyRangeMin)
            //    inRange = true;
            inRange = playerDist < enemyRangeMax && playerDist > enemyRangeMin;

            if (ai.CanSeePlayer())
            {
                if (inRange)
                {
                    //if player moving towards droid
                    if (Vector3.Angle(playerDirection, playerVelocity) > 160f)
                        motor.movementDirection = playerVelocity.normalized * 0.6f;
                    else
                    {
                        motor.movementDirection = Vector3.zero;
                    }

                    //weapon.Seek(player);
                    //weapon.Fire(player);
                }
                else
                {
                    //if (playerDist < enemyRangeMin)
                    //    motor.movementDirection = -playerDirection;
                    //else
                    //    motor.movementDirection = playerDirection;
                    motor.movementDirection = playerDirection * (playerDist < enemyRangeMin ? -0.6f : 1);
                }

                if (playerDist < enemyRangeMax)
                {
                    weapon.Seek(player);
                    weapon.Fire(player);
                }

                lostPlayerTime = -1f;
                lastPlayerPosition = ai.player.position;
            }
            else
            {
                if (lostPlayerTime < 0)
                    lostPlayerTime = Time.time;

                //player just disappeared move to last known location
                Vector3 lastPlayerDirection = (lastPlayerPosition - ai.character.position).normalized;
                motor.movementDirection = lastPlayerDirection;
                motor.facingDirection = lastPlayerDirection;

                //if (Time.time > lostPlayerTime + 3f)
                if (lostPlayerTime > 0 && Time.time > lostPlayerTime + 3f)
                {
                    lostPlayerTime = -1f;
                    ai.OnLostTrack();
                }
            }

            motor.facingDirection = playerDirection;
        }
    }
}