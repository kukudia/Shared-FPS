using UnityEngine;
using System.Collections;

namespace GravityBox.Utils
{
    /// <summary>
    /// used to remove or hide Particles that are no longer playing
    /// </summary>
    public class ParticleSystemController : MonoBehaviour 
    {
        public float checkFrequency = 0.5f;
	    public bool OnlyDeactivate;
        private ParticleSystem particles;

        void OnEnable()
        {
            particles = GetComponent<ParticleSystem>();
            StartCoroutine("CheckIfAlive");
        }
	
	    IEnumerator CheckIfAlive ()
	    {
            while (true)
            {
                yield return new WaitForSeconds(checkFrequency);
                if (!particles.IsAlive(true))
                {
                    if (OnlyDeactivate)
                        gameObject.SetActive(false);
                    else
                        Destroy(gameObject);
                    yield break;
                }
            }
	    }
    }
}