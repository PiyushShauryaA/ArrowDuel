using UnityEngine;

[ExecuteAlways]
public class PixelPerfectCamera : MonoBehaviour
{
    [SerializeField] int pixelsPerUnit = 100;
    [SerializeField] bool enableOnMobileOnly = true;

    void Update()
    {
#if UNITY_WEBGL
        if (!enableOnMobileOnly || Application.isMobilePlatform) {
            Camera.main.orthographicSize = 
                Screen.height / (2f * pixelsPerUnit);
        }
#endif
    }
}
