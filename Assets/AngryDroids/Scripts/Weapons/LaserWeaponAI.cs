using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    public class LaserWeaponAI : WeaponAI
    {
        public Transform[] laserTransforms;
        public GameObject laserPrefab;
        
        private GameObject[] laserRays;

        protected override void OnAwake()
        {
            base.OnAwake();

            laserRays = new GameObject[laserTransforms.Length];
            for (int i = 0; i < laserTransforms.Length; i++)
            {
                laserRays[i] = Instantiate(laserPrefab, laserTransforms[i].position, laserTransforms[i].rotation) as GameObject;
                laserRays[i].transform.parent = laserTransforms[i];
            }
        }

        protected override IEnumerator FireCoroutine()
        {
            foreach (GameObject ray in laserRays)
                ray.SetActive(true);
            
            while (laserRays[0].activeSelf)
                yield return null;

            isFiring = false;
            yield return new WaitForSeconds(cooldown);
        }        
    }
}