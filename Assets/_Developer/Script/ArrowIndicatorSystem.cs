using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArrowIndicatorSystem : MonoBehaviour
{

    public static ArrowIndicatorSystem instance;

    [Header("Indicator Settings")]
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private Transform indicatorParent;
   // [SerializeField] private float minIndicatorSize = 0.5f;
   // [SerializeField] private float maxIndicatorSize = 1.5f;
    [SerializeField] private Color playerArrowColor = Color.green;
    [SerializeField] private Color aiArrowColor = Color.red;

    private Camera mainCamera;
    public RectTransform canvasRect;
    private Dictionary<GameObject, GameObject> arrowIndicators = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        instance = this;
        mainCamera = Camera.main;
    }

    private void Update()
    {
        CleanUpIndicators();

        foreach (var pair in new Dictionary<GameObject, GameObject>(arrowIndicators))
        {
            UpdateIndicator(pair.Key, pair.Value);
        }
    }

    public void TrackArrow(GameObject arrow, bool isPlayerArrow)
    {
        if (arrowIndicators.ContainsKey(arrow)) return;

        GameObject indicator = Instantiate(indicatorPrefab, indicatorParent);
        Image indicatorImage = indicator.GetComponent<Image>();
        indicatorImage.color = isPlayerArrow ? playerArrowColor : aiArrowColor;

        arrowIndicators.Add(arrow, indicator);
    }

    private void UpdateIndicator(GameObject arrow, GameObject indicator)
    {
        Vector3 screenPos = mainCamera.WorldToViewportPoint(arrow.transform.position);

        // Arrow is on screen
        if (screenPos.x >= 0 && screenPos.x <= 1 && screenPos.y >= 0 && screenPos.y <= 1)
        {
            indicator.SetActive(false);
            return;
        }

        indicator.SetActive(true);

        // Calculate indicator position
        Vector2 indicatorPos = new Vector2(
            Mathf.Clamp(screenPos.x, 0.0125f, 0.9875f),
            Mathf.Clamp(screenPos.y, 0.0125f, 0.9875f)
        );

        // Convert to canvas position
        indicatorPos.x = (indicatorPos.x * canvasRect.sizeDelta.x) - (canvasRect.sizeDelta.x * 0.5f);
        indicatorPos.y = (indicatorPos.y * canvasRect.sizeDelta.y) - (canvasRect.sizeDelta.y * 0.5f);

        // Apply position with offset
        indicator.GetComponent<RectTransform>().anchoredPosition = indicatorPos;

        // Calculate distance-based size
        /*float distance = Vector3.Distance(mainCamera.transform.position, arrow.transform.position);
        float size = Mathf.Lerp(maxIndicatorSize, minIndicatorSize, distance / 50f);
        indicator.transform.localScale = new Vector3(size, size, size);*/

        // Rotate indicator to point toward arrow
        Vector3 dir = (arrow.transform.position - mainCamera.transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        indicator.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    private void CleanUpIndicators()
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var pair in arrowIndicators)
        {
            if (pair.Key == null)
            {
                Destroy(pair.Value);
                toRemove.Add(pair.Key);
            }
        }

        foreach (var key in toRemove)
        {
            arrowIndicators.Remove(key);
        }
    }

}
