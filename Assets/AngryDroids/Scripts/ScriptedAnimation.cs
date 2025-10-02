using UnityEngine;

namespace GravityBox
{
    public abstract class ScriptedAnimation : MonoBehaviour
    {
        public float animationTime = 0f;
        public OnAnimationEnd onAnimationEnd;

        float time = 0;
        float normalizedTime = 0;

        public enum OnAnimationEnd
        {
            DoNothing,
            Loop,
            DisableScript,
            HideGameObject,
            DestroyGameObject
        }

        void Awake()
        {
            OnAwake();
        }

        protected virtual void OnAwake() { }

        void OnEnable()
        {
            time = 0;
            OnEnableScript();
        }

        protected virtual void OnEnableScript() { }

        void OnDisable()
        {
            OnDisableScript();
        }

        protected virtual void OnDisableScript() { }

        void OnDestroy()
        {
            OnDestroyGameObject();
        }

        protected virtual void OnDestroyGameObject() { }

        void Update()
        {
            //handle time
            time += Time.deltaTime;
            normalizedTime = time / animationTime;

            //update effect
            UpdateAnimation(normalizedTime);

            if (normalizedTime > 1)
                EndAnimation();
        }

        protected virtual void UpdateAnimation(float normalizedTime) { }

        public void EndAnimation()
        {
            switch (onAnimationEnd)
            {
                case OnAnimationEnd.Loop:
                    time = 0;
                    break;

                case OnAnimationEnd.DisableScript:
                    enabled = false;
                    break;

                case OnAnimationEnd.HideGameObject:
                    gameObject.SetActive(false);
                    break;

                case OnAnimationEnd.DestroyGameObject:
                    Destroy(gameObject);
                    break;

                default:
                    break;
            }
        }
    }
}