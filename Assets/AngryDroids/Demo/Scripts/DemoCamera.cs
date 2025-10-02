using UnityEngine;

namespace GravityBox.AngryDroids.Demo
{
    /// <summary>
    /// Demo camera similar to simple RTS camera
    /// controlled with mouse and directional keys
    /// </summary>
    public class DemoCamera : MonoBehaviour
    {
        public Transform target = null;

        public float sensitivity = 10f;
        public float borderPixels = 20;
        public bool limitCameraPosition = false;
        public Vector3 positionLimitMin = Vector3.zero;
        public Vector3 positionLimitMax = Vector3.zero;

        private Vector2 screenSize;

        private void OnEnable()
        {
            screenSize = new Vector2(Screen.width, Screen.height);
        }

        private void Update()
        {
            Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            if (Input.mousePosition.x < borderPixels)
                input.x += -1;
            else if (Input.mousePosition.x > screenSize.x - borderPixels)
                input.x += 1;

            if (Input.mousePosition.y < borderPixels)
                input.y += -1;
            else if (Input.mousePosition.y > screenSize.y - borderPixels)
                input.y += 1;

            input = transform.TransformDirection(input);
            input.y = 0;
            input.Normalize();

            Vector3 newPosition = Vector3.Lerp(transform.position, transform.position + input, sensitivity * Time.deltaTime);
            transform.position = limitCameraPosition ? Clamp(newPosition, positionLimitMin, positionLimitMax) : newPosition;
        }

        private void OnGUI()
        {
            GUI.TextField(new Rect(10, 10, 200, 20), string.Format("{0}/{1}", screenSize.ToString(), Input.mousePosition));// Input.mousePosition.ToString());
        }

        private void OnDrawGizmosSelected()
        {
            Bounds bounds = new Bounds();
            bounds.SetMinMax(positionLimitMin, positionLimitMax);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private static Vector3 Clamp(Vector3 vector, Vector3 min, Vector3 max)
        {
            return new Vector3(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y),
                Mathf.Clamp(vector.z, min.z, max.z));
        }
    }
}