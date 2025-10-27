using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Projectiles
{
    public class CheckReady : MonoBehaviour
    {
        public NetworkRunner runner;
        public bool gameStart = false;
        public bool canStart = true;

        private void Update()
        {
            if (gameStart && canStart)
            {
                OnGameStart();
            }
        }

        public void OnGameStart()
        {
            canStart = false;
            //runner.LoadScene(SceneRef);
        }
    }
}