using UnityEngine;

namespace GravityBox.AngryDroids
{
    [System.Serializable]
    public class WeaponSeek
    {
        public bool headSeek;
        public bool weaponSeek;
        public float seekSpeed = 1;

        //public Transform target;
        public Transform root;
        public Transform head;
        public Transform weapon;

        public Vector2 headLimits;
        public Vector2 weaponLimits;

        private Quaternion initialHeadRotation;
        private Quaternion initialWeaponRotation;
        private Quaternion frameHeadRotation;
        private Quaternion frameWeaponRotation;

        public void Awake()
        {
            if (!headSeek && !weaponSeek) return;
            initialHeadRotation = head.localRotation;
            initialWeaponRotation = weapon.localRotation;
            frameHeadRotation = head.localRotation;
            frameWeaponRotation = weapon.localRotation;
        }

        //if you want to override animation of head and weapon
        //call this method in LateUpdate
        public void SeekOld(Vector3 point)
        {
            if (!headSeek && !weaponSeek) return;
            point = root.InverseTransformPoint(point);
            if (headSeek)
            {
                Vector3 headTargetVector = point - root.InverseTransformPoint(head.position);

                Quaternion headRotation = Quaternion.LookRotation(headTargetVector);
                headRotation = Clamp(headRotation, headLimits);
                //if (seekSpeed > 0)
                //{
                //    head.localRotation = Quaternion.Lerp(frameHeadRotation, initialHeadRotation * headRotation, seekSpeed * Time.deltaTime);
                //    frameHeadRotation = head.localRotation;
                //}
                //else
                    head.localRotation = initialHeadRotation * headRotation;
            }
            if (weaponSeek)
            {
                Vector3 weaponTargetVector = point - root.InverseTransformPoint(weapon.position);

                Quaternion weaponRotation = Quaternion.LookRotation(weaponTargetVector);
                weaponRotation = Clamp(weaponRotation, weaponLimits);
                //if (seekSpeed > 0)
                //{
                //    weapon.localRotation = Quaternion.Lerp(frameWeaponRotation, initialWeaponRotation * weaponRotation, seekSpeed * Time.deltaTime);
                //    frameWeaponRotation = weapon.localRotation;
                //}
                //else
                    weapon.localRotation = initialWeaponRotation * weaponRotation;
            }

            Debug.DrawRay(weapon.position, weapon.forward*10, Color.red);
            Debug.DrawRay(head.position, head.forward*10, Color.blue);
            Debug.DrawLine(weapon.position, root.TransformPoint(point));
        }

        public void Seek(Vector3 point)
        {
            if (!headSeek && !weaponSeek) return;
            
            if (headSeek)
            {
                head.LookAt(point);
                Quaternion headRotation = Quaternion.Inverse(initialHeadRotation) * head.localRotation;
                headRotation = Clamp(headRotation, headLimits);
                head.localRotation = Quaternion.Lerp(frameHeadRotation, initialHeadRotation * headRotation, seekSpeed * Time.deltaTime);
                frameHeadRotation = head.localRotation;

                //head.localRotation = initialHeadRotation * headRotation;
            }

            if (weaponSeek)
            {
                weapon.LookAt(point);
                Quaternion weaponRotation = Quaternion.Inverse(initialWeaponRotation) * weapon.localRotation;
                weaponRotation = Clamp(weaponRotation, weaponLimits);
                weapon.localRotation = Quaternion.Lerp(frameWeaponRotation, initialWeaponRotation * weaponRotation, seekSpeed * Time.deltaTime);
                frameWeaponRotation = weapon.localRotation;

                //weapon.localRotation = initialWeaponRotation * weaponRotation;
            }

            Debug.DrawRay(weapon.position, weapon.forward * 10, Color.red);
            Debug.DrawRay(head.position, head.forward * 10, Color.blue);
        }

        static Quaternion Clamp(Quaternion q, Vector3 bounds)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
            angleX = Mathf.Clamp(angleX, -bounds.x, bounds.x);
            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            float angleY = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.y);
            angleY = Mathf.Clamp(angleY, -bounds.y, bounds.y);
            q.y = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleY);

            float angleZ = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.z);
            angleZ = Mathf.Clamp(angleZ, -bounds.z, bounds.z);
            q.z = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleZ);

            return q.normalized;
        }
    }
}