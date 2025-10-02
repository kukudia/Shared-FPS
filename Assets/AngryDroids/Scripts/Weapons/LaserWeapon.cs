using UnityEngine;
using System.Collections;

namespace GravityBox.AngryDroids
{
    [System.Serializable]
    public class FXSocket
    {
        public enum Mode
        {
            All,
            OneByOne,
            RandomOrder,
            CustomOrder
        }

        public Mode fireMode = Mode.All;
        public GameObject prefab;
        public Transform[] sockets;
        private GameObject[] objects;

        public void Init()
        {
            if (objects == null)
            {
                objects = new GameObject[sockets.Length];
                for (int i = 0; i < sockets.Length; i++)
                {
                    objects[i] = Object.Instantiate(prefab, sockets[i].position, sockets[i].rotation) as GameObject;
                    objects[i].transform.parent = sockets[i];
                }
            }
        }

        public void Enable()
        {
            if (fireMode == Mode.All)
            {
                foreach (GameObject obj in objects)
                    obj.SetActive(true);
            }
        }

        public bool IsActive()
        {
            if (fireMode == Mode.All)
            {
                if (objects[0].activeSelf)
                    return true;
            }

            return false;
        }
    }

    public class LaserWeapon : Weapon
    {
        public FXSocket fxs = new FXSocket();
        public Transform[] laserTransforms;
        public GameObject laserPrefab;

        protected override void OnAwake()
        {
            base.OnAwake();
            fxs.Init();
        }

        protected override IEnumerator FireCoroutine()
        {
            fxs.Enable();
            enabled = true;

            while (fxs.IsActive())
                yield return null;

            isFiring = false;
            yield return new WaitForSeconds(cooldown);

            enabled = false;
        }        
    }
}