using UnityEngine;

namespace GravityBox.AngryDroids
{
    /// <summary>
    /// Attack behaviour for walking kinds of droids
    /// </summary>
    public class WalkerAttackMoveController : AIBehaviour
    {
        public float enemyRangeMin = 0f;
        public float enemyRangeMax = 100f;

        public float strafeSnappyness = 0.8f;
        public float retreatSnappyness = 0.6f;

        private bool inRange = false;
        private float noticePlayerTime = -0.7f;
        private float lostPlayerTime = -1f;
        private Vector3 lastPlayerPosition = Vector3.zero;

        void OnEnable()
        {
            inRange = false;
            lostPlayerTime = -1f;
            noticePlayerTime = Time.time;

            //if for some reason ai is not active
            if (!ai.IsActive())
                ai.Activate(true);
        }

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
            if (ai.player!=null && ai.IsPlayerDead())
                ai.OnLostTrack();
        }

        void Move() 
        {
            if (Time.time < noticePlayerTime + 0.8f)
            {
                motor.movementDirection = Vector3.zero;
                return;
            }

            Vector3 playerDirection = ai.GetPlayerDirection(true);
            Vector3 playerVelocity = ai.GetPlayerVelocity();

            float playerDist = playerDirection.magnitude;
            playerDirection /= playerDist;

            //inRange = playerDist < weapon.rangeMax && playerDist > weapon.rangeMin;
            inRange = playerDist < enemyRangeMax && playerDist > enemyRangeMin;

            if (ai.CanSeePlayer()) //player visible
            {
                if (inRange)
                {
                    motor.movementDirection = Vector3.zero;
                    //if player moving towards droid
                    if (playerVelocity.sqrMagnitude > 0.1f && Vector3.Angle(playerDirection, playerVelocity) > 160f)
                    {
                        motor.movementDirection = playerVelocity.normalized * retreatSnappyness;
                    }
                    else
                    {
                        //if player is about to aim, do strafe
                        if (Vector3.Angle(player.forward, playerDirection) > 160f)
                        {
                            motor.movementDirection = transform.right * (Mathf.PingPong(Time.time, 2.0f) - 1f) * strafeSnappyness;
                        }
                    }

                    ////if player moving towards droid
                    //if (Vector3.Angle(playerDirection, playerVelocity) > 160f)
                    //    motor.movementDirection = playerVelocity.normalized * retreatSnappyness;
                    //else
                    //{
                    //    //if player is about to aim, do strafe
                    //    if (Vector3.Angle(player.forward, playerDirection) > 160f)
                    //    {
                    //        motor.movementDirection = transform.right * (Mathf.PingPong(Time.time, 2.0f) - 1f) * strafeSnappyness;
                    //    }
                    //    else
                    //        motor.movementDirection = Vector3.zero;
                    //}
                }
                else
                {
                    //motor.movementDirection = playerDirection * (playerDist < weapon.rangeMin ? -0.6f : 1);
                    motor.movementDirection = playerDirection * (playerDist < enemyRangeMin ? -0.6f : 1);
                }

                //if (playerDist < weapon.rangeMax) 
                if (playerDist < enemyRangeMax) 
                {
                    weapon.Seek(player);
                    weapon.Fire(player);
                }

                lostPlayerTime = -1f;
                lastPlayerPosition = ai.player.position;
            }          
            else //player is not visible
            {
                if (lostPlayerTime < 0)
                    lostPlayerTime = Time.time;

                //player just disappeared move to last known location
                //if (lastPlayerPosition != Vector3.zero)
                //{
                Vector3 lastPlayerDirection = (lastPlayerPosition - ai.character.position).normalized;
                motor.movementDirection = lastPlayerDirection;
                motor.facingDirection = lastPlayerDirection;
                //}

                if (lostPlayerTime > 0 && Time.time > lostPlayerTime + 3f)
                {
                    lostPlayerTime = -1f;
                    lastPlayerPosition = Vector3.zero;
                    ai.OnLostTrack();
                }
            }

            motor.facingDirection = playerDirection;
        }
    }
}