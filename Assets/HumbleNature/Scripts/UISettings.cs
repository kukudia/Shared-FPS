using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISettings : MonoBehaviour
{

    [SerializeField]
    private Animator settings;

    [SerializeField]
    private Camera mainCamera;
    [SerializeField]
    private SimpleCameraEffects effects;

    [SerializeField]
    private Text txtHour;
    [Space(10)]
    [SerializeField]
    private GameObject joystick;
    [SerializeField]
    private GameObject postProcessing;
    [Space(10)]
    [SerializeField]
    private List<GameObject> trees = new List<GameObject>();
    [SerializeField]
    private List<GameObject> clouds = new List<GameObject>();
    [SerializeField]
    private List<GameObject> rocks = new List<GameObject>();
    [Space(10)]
    [Header("UI")]
    [SerializeField]
    private Slider graphics;
    [SerializeField]
    private Toggle tickFPS;
    [SerializeField]
    private Toggle tickCinematic;

     [Space(10)]
    [SerializeField]
    private GameObject fps;
    [SerializeField]
    private GameObject cinematic;
    [SerializeField]
    private SkyManager sky;

    [SerializeField]
    private Image fadeScreen;

    private bool onPause;

    private void Start()
    {
        Application.targetFrameRate = 60;
        Cursor.visible = false;
        
        joystick.SetActive(false);

#if UNITY_IPHONE
        joystick.SetActive(true);
#endif
#if UNITY_ANDROID
        joystick.SetActive(true);
#endif

        graphics.maxValue = QualitySettings.names.Length-1;
        ChangeQualityLvl(graphics.maxValue-1);

        if (fadeScreen)
            StartCoroutine(FadeImage());

        ChangeToCinematic(true);
        ChangeSimpleCameraEffect(false);
        ChangeView(900);
    }

    IEnumerator FadeImage()
    {
        fadeScreen.color = new Color(0, 0, 0, 1);
        yield return new WaitForSeconds(0.1f);

        while(fadeScreen.color.a > 0.1f)
        {
            
            fadeScreen.color = Color.Lerp(fadeScreen.color, new Color(0, 0, 0, 0), Time.deltaTime );
            yield return null;
        }

        fadeScreen.color = new Color(0, 0, 0, 0);

    }

    public void ChangeQualityLvl(float i)
    {
        int value = Mathf.RoundToInt(i);
        QualitySettings.SetQualityLevel(value, true);
        PlayerPrefs.SetInt("Graphics", value);
        graphics.value = value;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Cursor.visible = !Cursor.visible;

            if (Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;

                if (onPause)
                {
                    ResumeButtonPressed();
                }
            }

        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg)
            {
                if (cg.alpha == 1)
                {
                    cg.alpha = 0;
                }
                else
                {
                    cg.alpha = 1;
                }
            }    
        }

        if (SkyManager.instance)
        {
            txtHour.text = "Hour (" + Mathf.RoundToInt(SkyManager.instance.hour) + ")";
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void ChangeToFPS(bool value)
    {
        if (value)
        {
            fps.SetActive(true);
            cinematic.SetActive(false);
            sky.SetTarget(fps.GetComponentInChildren<Camera>().transform);
            tickCinematic.isOn = false;
        }

    }

    public void ChangeToCinematic(bool value)
    {
        if (value)
        {
            fps.SetActive(false);
            cinematic.SetActive(true);
            sky.SetTarget(cinematic.GetComponentInChildren<Camera>().transform);
            tickFPS.isOn = false;
        }


    }

    public void ChangeDaySpeed(float value)
    {
        if (SkyManager.instance)
        {
            SkyManager.instance.speed = value;
        }
    }

    public void ChangeDayHour(float value)
    {
        if (SkyManager.instance)
        {
            SkyManager.instance.hour = value;
        }
    }

    public void ChangeAzimuth(float value)
    {
        if (SkyManager.instance)
        {
            SkyManager.instance.azimuth = value;
        }
    }

    public void ChangeAltitude(float value)
    {
        if (SkyManager.instance)
        {
            SkyManager.instance.altitude = value;
        }
    }

    public void ChangeSimpleCameraEffect(bool value)
    {
        TurnOnOFF_DirtyLens(value);
        TurnOnOFF_Light(value);
        TurnOnOFF_Shadow(value);
    }

    public void ChangePP(bool value)
    {
        postProcessing.gameObject.SetActive(value);
    }

    public void ChangeFOV(float value)
    {
        mainCamera.fieldOfView = value;
    }

    public void ChangeView(float value)
    {
        mainCamera.farClipPlane = value;
    }

    public void TurnOnOFF_Trees(bool value)
    {
        foreach (var g in trees)
        {
            g.SetActive(value);
        }
    }

    public void TurnOnOFF_Clouds(bool value)
    {
        foreach (var g in clouds)
        {
            g.SetActive(value);
        }
    }

    public void TurnOnOFF_Rocks(bool value)
    {
        foreach (var g in rocks)
        {
            g.SetActive(value);
        }
    }

    public void TurnOnOFF_DirtyLens(bool value)
    {
        effects.lensSettings.active = value;
    }

    public void TurnOnOFF_Light(bool value)
    {
        effects.lightSettings.active = value;
    }
    public void TurnOnOFF_Shadow(bool value)
    {
        effects.groundShadowSettings.active = value;
    }


    public void SettingsButtonPressed()
    {
        settings.SetBool("Show", true);
        onPause = true;
        Time.timeScale = 0;
    }

  
    public void ResumeButtonPressed()
    {
        settings.SetBool("Show", false);
        onPause = false;
        Time.timeScale = 1;
    }

}
