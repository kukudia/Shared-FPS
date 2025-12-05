using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SkyManager : MonoBehaviour
{
    public Light directionalLight;

    [Space(10)]
    [Range(0, 360)]
    public float azimuth;
    [Range(0, 360)]
    public float altitude;

    [Range(0.0f, 24.0f)]
    public float hour;
    public float speed = 2;

    [Space(10)]
    [Tooltip("This object will follow the target")]
    [SerializeField]
    private Transform target;

    [SerializeField]
    private HourSetting[] hourSettings = new HourSetting[1];

    [SerializeField]
    private List<Transform> godRays = new List<Transform>();

    [SerializeField]
    private Vector3 rayOffset;

   private float angle;
    private float shadowStrength = 0;

    public static SkyManager instance;

    private void Awake()
    {
        if (instance)
        {
            instance.gameObject.SetActive(false);
        }

        instance = this;
        RenderSettings.sun = directionalLight;
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            hour = hour +  Time.unscaledDeltaTime * speed;
        }
        
        if (hour > 24)
        {
            hour = 0;
        }

        ApplyHourSettings();

        angle = (hour * 1.5f) *10 - 90;

        directionalLight.transform.localRotation = Quaternion.Euler(angle, azimuth, altitude);




    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
        {
            if (!target)
                return;

            transform.position = target.position;
        }

        foreach (var g in godRays)
        {
            g.eulerAngles = directionalLight.transform.eulerAngles + rayOffset;
        }
    }

    public void SetTarget(Transform value)
    {
        target = value;
    }

    void ApplyHourSettings()
    {
        int i;

        for (i = 0; i <= hourSettings.Length - 1; i++)
        {
            if (hour >= hourSettings[i].startHour && hour < hourSettings[i].endHour)
            {
                float minSpeed = Mathf.Clamp(speed, 1, float.MaxValue);

                if (directionalLight)
                {
                   
                    float r = Mathf.Lerp(directionalLight.color.r, hourSettings[i].lightColor.r,  Time.unscaledDeltaTime * (hourSettings[i].changeSpeed * minSpeed));
                    float g = Mathf.Lerp(directionalLight.color.g, hourSettings[i].lightColor.g,  Time.unscaledDeltaTime * (hourSettings[i].changeSpeed * minSpeed)); 
                    float b = Mathf.Lerp(directionalLight.color.b, hourSettings[i].lightColor.b,  Time.unscaledDeltaTime * (hourSettings[i].changeSpeed * minSpeed)); 

                    directionalLight.color = new Color(r, g, b, 1);

                    shadowStrength = Mathf.Lerp(shadowStrength, hourSettings[i].shadowAmount,  Time.unscaledDeltaTime * (hourSettings[i].changeSpeed * minSpeed));
                    directionalLight.shadowStrength = shadowStrength;

                }

                if (hourSettings[i].changeFog)
                {
                    RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, hourSettings[i].fogColor,  Time.unscaledDeltaTime * (hourSettings[i].changeSpeed * minSpeed));
                    RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, hourSettings[i].fogIntensity,  Time.unscaledDeltaTime * (hourSettings[i].changeSpeed * minSpeed));

                }

                RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, hourSettings[i].ambientGroundColor,  Time.unscaledDeltaTime * (hourSettings[i].changeSpeed * minSpeed));
            }
        }

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(directionalLight.transform.position, directionalLight.transform.position + (directionalLight.transform.forward * 3));
        Gizmos.color = directionalLight.color;
        Gizmos.DrawSphere(directionalLight.transform.position, 1f);
        Update();
    }
}


[System.Serializable]
public class HourSetting
{
    public string name;
    [Range(0.0f, 24.0f)]
    public float startHour;
    [Range(0.0f, 24.0f)]
    public float endHour;
    public Color lightColor;
    [Range(0.0f, 1.0f)]
    public float shadowAmount;
    [Space(10)]
    public Color ambientGroundColor = Color.white;
    [Space(10)]
    public bool changeFog;
    public Color fogColor = Color.white;
    public float fogIntensity = 0.01f;

    [Space(10)]
    [Header("---------------------------")]
    public float changeSpeed = 1;

}
