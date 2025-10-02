using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GravityBox.AngryDroids.Demo
{
    /// <summary>
    /// Simple demo gameplay logic
    /// Select droid to play, game over and select again
    /// </summary>
    public class DemoLogic : MonoBehaviour
    {
        public int droidsCount = 3;
        public float countdownTime = 2f;

        public Transform demoTarget;
        public Transform demoCamera;
        public GameObject spawner;
        public GameObject menu;
        public GameObject[] prefabs;
        public Button playButton;

        private Vector3 startGameCameraPosition;
        private Vector3 startGameTargetPosition;

        public int droidType { get; set; }
        public int weaponType { get; set; }

        private void Start()
        {
            startGameTargetPosition = demoTarget.position;
            startGameCameraPosition = demoCamera.position;
            menu.SetActive(true);
        }

        public void StartGame()
        {
            StartCoroutine(StartGameCoroutine());
        }

        IEnumerator StartGameCoroutine()
        {
            int prefabId = weaponType * droidsCount + droidType;
            menu.SetActive(false);

            SpawnEffectController spawnEffect = spawner.GetComponent<SpawnEffectController>();
            spawnEffect.spawn = prefabs[prefabId];
            spawnEffect.spawned = null;
            spawner.SetActive(true);
            
            while (spawnEffect.spawned == null)
                yield return null;

            GameObject player = (GameObject)spawnEffect.spawned;
            player.GetComponent<Health>().onDeath += OnPlayerDead;
            demoCamera.GetComponent<DemoCamera>().target = player.transform;
            demoCamera.GetComponent<DemoCamera>().enabled = true;
        }

        private void OnPlayerDead()
        {
            StartCoroutine(RestartCoroutine());
        }

        IEnumerator RestartCoroutine()
        {
            demoCamera.GetComponent<DemoCamera>().enabled = false;
            demoTarget.position = startGameTargetPosition;

            yield return new WaitForSeconds(1f);

            Vector3 demoCameraPosition = demoCamera.position;
            float countdown = countdownTime;
            while (countdown > 0f)
            {
                countdown -= Time.deltaTime;
                Vector3 position = Vector3.Lerp(startGameCameraPosition, demoCameraPosition, (countdown / countdownTime));
                demoCamera.position = position;
                yield return null;
            }

            menu.SetActive(true);
        }
    }
}