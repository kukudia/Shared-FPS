using UnityEngine;

namespace GravityBox.AngryDroids.Demo
{
    /// <summary>
    /// Droid controller for player
    /// TODO: It woud be cool to make player posess any droid he selects
    /// </summary>
    public class DemoDroidUserControl : MonoBehaviour
    {
        public bool move;
        public float hideArrowAt = 2f;

        private DemoDroidActivator droid;
        private IMovementMotor motor;
        private IWeapon weapon;
        
        private static Transform positionMark;
        private static Transform attackMark;

        private Transform attackTarget;
        private IHealth attackHealth;

        private RaycastHit hit;
        
        void Start()
        {
            if (positionMark == null)
            {
                positionMark = GameObject.Find("DemoUserInputTarget").transform;
                positionMark.gameObject.SetActive(false);
            }

            if (attackMark == null)
            {
                attackMark = GameObject.Find("DemoUserAttackTarget").transform;
                attackMark.gameObject.SetActive(false);
            }

            droid = GetComponent<DemoDroidActivator>();
            motor = GetComponent<IMovementMotor>();
            weapon = GetComponent<IWeapon>();
        }

        private void OnDisable()
        {
            if(positionMark != null)
                positionMark.gameObject.SetActive(false);
            if(attackMark != null)
                attackMark.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!droid.isActive)
                    droid.Activate(true);

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, 100, 1 << 8)) //AI layer is used for all enemies, it will not collide with triggers in demo
                {
                    if (hit.transform.GetComponent<IHealth>() != null)
                    {
                        attackTarget = hit.transform;
                        attackHealth = hit.transform.GetComponent<IHealth>();
                        Vector3 toPlayer = transform.position - attackTarget.position;
                        float dist = Mathf.Min((weapon.rangeMax + weapon.rangeMin) / 2f, toPlayer.magnitude);
                        //TODO: before updating position ask a weapon if target can be reached from here 
                        positionMark.position = attackTarget.position + toPlayer.normalized * dist;
                    }
                }
                else if (Physics.Raycast(ray, out hit, 100, 1 << 5)) //UI layer is used for ground (droid orientation), it will not collide with triggers in demo
                {
                    positionMark.position = hit.point;
                }
            }

            if (droid.isActive && droid.isReady)
            {
                Vector3 position = transform.position;
                Vector3 targetPos = positionMark.position;
                Vector3 walkDir = targetPos - position;
                walkDir.y = 0;
                walkDir.Normalize();

                if (move)
                {
                    motor.movementTarget = positionMark.position;
                }

                if (attackTarget != null)
                {
                    //weapon will actually fire as soon as player is reachable
                    //for more sofisticated behaviour this needs more logic
                    weapon.Seek(attackTarget);
                    weapon.Fire(attackTarget);

                    Vector3 attackPos = attackTarget.position;
                    Vector3 lookDir = attackPos - position;
                    lookDir.y = 0;
                    lookDir.Normalize();

                    if (attackHealth.Dead)
                    {
                        attackTarget = null;
                        attackHealth = null;
                    }
                    else
                        motor.facingDirection = lookDir;

                    if (Vector3.Angle(lookDir, walkDir) > 90f) //that's where droids sometimes loose target
                    {
                        attackTarget = null;
                        attackHealth = null;
                    }
                }
                else
                    motor.facingDirection = walkDir;
            }
            
            positionMark.gameObject.SetActive(Vector3.Distance(transform.position, positionMark.position) > hideArrowAt);
            attackMark.gameObject.SetActive(attackTarget != null);
            attackMark.position = attackTarget != null ? attackTarget.position : Vector3.zero;
        }
    }
}
