using UnityEngine;
using UnityEngine.Events;

namespace GravityBox.AngryDroids.Demo
{
    /// <summary>
    /// Primitive trigger to make at least 
    /// basic gameplay functionality possible
    /// </summary>
    public class DemoTrigger : MonoBehaviour
    {
        public UnityEvent onEnter;
        public UnityEvent onExit;
        public bool triggeredOnce = true;
        private bool triggered;

        void OnTriggerEnter(Collider other)
        {
            if (other.tag != "Player" || triggered) return;
            
            onEnter?.Invoke();

            triggered = true;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.tag != "Player") return;

            onExit?.Invoke();

            if (!triggeredOnce)
                triggered = false;
        }
    }
}