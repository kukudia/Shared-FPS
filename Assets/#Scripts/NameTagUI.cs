using UnityEngine;
using TMPro;

public class NameTagUI : MonoBehaviour
{
    public TMP_Text text;
    public Transform target;    // 玩家头顶位置
    public Vector3 offset = new Vector3(0, 2f, 0);

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null || cam == null) return;

        // 世界 → 屏幕
        Vector3 screenPos = cam.WorldToScreenPoint(target.position + offset);

        // 在相机背后：隐藏
        if (screenPos.z < 0)
        {
            text.enabled = false;
            return;
        }

        text.enabled = true;
        transform.position = screenPos;
    }
}
