using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCameraEffects : MonoBehaviour
{
    [System.Serializable]
    public class GroundShadow
    {
        public bool active = true;

        public Color color;

        [SerializeField]
        private UnityEngine.UI.Image shadow;

        [SerializeField]
        [Range(0, 1f)]
        private float maxAlpha = 0.6f;

        [SerializeField]
        [Range(0, 170f)]
        private float detectionAngle = 180f;


        public void Apply(Transform sunLight)
        {

            if (shadow)
            {

                float perc = 1;

                if (!shadow.gameObject.activeSelf)
                    shadow.gameObject.SetActive(true);

                if (!sunLight)
                    return;

                float angle;

                angle = Mathf.Abs(Vector3.Angle(Camera.main.transform.forward, sunLight.transform.forward));

                angle = Mathf.Clamp(angle, 0, 180);

                perc = Mathf.Clamp(angle - detectionAngle, 0, 180 - detectionAngle) / (180 - detectionAngle);

                
                //---

                angle = Mathf.Abs(Vector3.Angle(Camera.main.transform.forward, Vector3.up));

                if (angle > 90)
                {
                    angle = Mathf.Abs(angle - 180);
                }

                shadow.color = new Color(color.r, color.g, color.b, maxAlpha * (perc * (angle / 90)));

            }

        }

        public void Disabled()
        {
            if (shadow)
                shadow.gameObject.SetActive(false);
        }
    }

    [System.Serializable]
    public class TopLight
    {
        public bool active = true;

        public LayerMask layers;
        [SerializeField]
        private UnityEngine.UI.Image light;

        [SerializeField]
        [Range(0, 1f)]
        private float maxAlpha = 0.6f;

        [HideInInspector]
        public bool isInDarkPlaces = false;

        private float currentMaxAlpha = 0;
        public void Apply(Transform origin)
        {
            if (!SkyManager.instance)
                return;

            if (light)
            {
                if (SkyManager.instance.hour > 17 || SkyManager.instance.hour < 7)
                {
                    isInDarkPlaces = true;
                }
                else
                {
                    RaycastHit hit;

                    if (Physics.Raycast(origin.position, Vector3.up, out hit, Mathf.Infinity, layers))
                    {
                        Debug.DrawRay(origin.position, Vector3.up * hit.distance, Color.yellow);
                        isInDarkPlaces = true;
                    }
                    else
                    {
                        Debug.DrawRay(origin.position, (origin.position + Vector3.up) * 1000, Color.white);
                        isInDarkPlaces = false;
                    }
                }


                if (isInDarkPlaces)
                {
                    if (currentMaxAlpha != 0)
                        currentMaxAlpha = Mathf.Lerp(currentMaxAlpha, 0, Time.deltaTime * 2);
                }
                else
                {
                    if (currentMaxAlpha != maxAlpha)
                        currentMaxAlpha = Mathf.Lerp(currentMaxAlpha, maxAlpha, Time.deltaTime * 2);
                }

                if (!light.gameObject.activeSelf)
                    light.gameObject.SetActive(true);

                float angle = Mathf.Abs(Vector3.Angle(Camera.main.transform.forward, Vector3.up));


                if (angle > 90)
                {
                    angle = Mathf.Abs(angle - 180);
                }


                light.color = new Color(light.color.r, light.color.g, light.color.b, currentMaxAlpha * (angle / 90));
            }
        }

        public void Disabled()
        {
            if (light)
                light.gameObject.SetActive(false);
        }
    }

    [System.Serializable]
    public class DirtyLens
    {
        public bool active = true;

        [SerializeField]
        private UnityEngine.UI.Image lens;

        [SerializeField]
        [Range(0, 1f)]
        private float maxAlpha = 0.6f;

        [SerializeField]
        [Range(0, 170f)]
        private float detectionAngle = 180f;

        float alpha = 0;
        public void Apply(Transform sunLight)
        {
            if (!SkyManager.instance)
                return;

            if (lens)
            {
                if (!lens.gameObject.activeSelf)
                    lens.gameObject.SetActive(true);


                if (!sunLight)
                    return;

                float angle = Mathf.Abs(Vector3.Angle(Camera.main.transform.forward, sunLight.transform.forward));

                angle = Mathf.Clamp(angle, 0, 180);

                float perc = Mathf.Clamp(angle - detectionAngle, 0, 180 - detectionAngle) / (180 - detectionAngle);


                if (SkyManager.instance.hour > 17 || SkyManager.instance.hour < 7)
                {
                    alpha = Mathf.Lerp(alpha, 0, Time.deltaTime * 2f);
                }
                else
                {
                    alpha = Mathf.Lerp(alpha, maxAlpha, Time.deltaTime * 2f);
                }


                lens.color = new Color(lens.color.r, lens.color.g, lens.color.b, alpha * perc);
            }

        }

        public void Disabled()
        {
            if (lens)
                lens.gameObject.SetActive(false);
        }
    }


    [Space(10)]
    public GroundShadow groundShadowSettings;

    [Space(10)]
    public TopLight lightSettings;

    [Space(10)]
    public DirtyLens lensSettings;

    public static SimpleCameraEffects instance;

    private void Awake()
    {
        instance = this;
    }


    void Update()
    {
        if (SkyManager.instance)
        {
            if (groundShadowSettings.active)
                groundShadowSettings.Apply(SkyManager.instance.directionalLight.transform);
            else
                groundShadowSettings.Disabled();


            if (lightSettings.active)
                lightSettings.Apply(transform);
            else
                lightSettings.Disabled();

            if (lensSettings.active)
                lensSettings.Apply(SkyManager.instance.directionalLight.transform);
            else
                lensSettings.Disabled();
        }
        else
        {
            groundShadowSettings.Disabled();
            lightSettings.Disabled();
            lensSettings.Disabled();
        }

    }
}
