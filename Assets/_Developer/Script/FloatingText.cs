using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingText : MonoBehaviour
{

    public int playerId = -1;

    public HeartDisplay heartDisplay;

    [Space(05)]
    public GameObject damageTextPrefab; // UI Prefab with CanvasRenderer
    public GameObject scoreTextPrefab; // UI Prefab with CanvasRenderer
    [Space(05)]
    public RectTransform canvasRectTransform;
    public RectTransform healthBarCanvas; // Reference to your UI canvas
    public RectTransform parent; // Reference to your UI canvas
    [Space(05)]
    public Vector3 textOffset = new Vector3(0, 5f, 0); // World space offset

    private Camera mainCamera;


    void Start()
    {
        mainCamera = Camera.main;
        InitFloatingText(ScoreManager.instance.heartDisplays[playerId]);

    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Plus))
        {

        }

        if (Input.GetKeyDown(KeyCode.Minus))
        {
            ShowDamageEffect();
        }

    }

    public void InitFloatingText(HeartDisplay _heartDisplay)
    {
        heartDisplay = _heartDisplay;
        healthBarCanvas = _heartDisplay.transform.parent.GetComponent<RectTransform>();
        canvasRectTransform = _heartDisplay.transform.root.GetComponent<RectTransform>();

    }

    private Vector2 GetWorldToScreenPosition()
    {
        // Convert world position to screen point
        Vector3 worldPosition = transform.position + textOffset;
        // Vector2 screenPosition = mainCamera.WorldToViewportPoint(worldPosition);

        // Convert world position to screen position
        Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        // Convert screen position to canvas local position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            screenPosition,
            null, // For Screen Space - Overlay use null, for Camera use the render camera
            out Vector2 localPoint
        );

        return localPoint;
    }

    public void ShowDamageEffect()
    {
        if (damageTextPrefab == null)
        {
            Debug.LogWarning("[FloatingText] damageTextPrefab is null!");
            return;
        }

        if (heartDisplay == null)
        {
            Debug.LogWarning($"[FloatingText] heartDisplay is null for playerId {playerId}!");
            return;
        }

        Vector2 localPoint = GetWorldToScreenPosition();

        RectTransform parent = null;
        int heartCount = 0;

        // Get heart count and ensure it's at least 1 (to avoid index 0 which becomes -1)
        if (playerId == 0)
        {
            heartCount = Mathf.Max(1, Mathf.RoundToInt(ScoreManager.instance.player1Hearts));
            parent = heartDisplay.GetTragetPoint(heartCount);
        }
        else if (playerId == 1)
        {
            heartCount = Mathf.Max(1, Mathf.RoundToInt(ScoreManager.instance.player2Hearts));
            parent = heartDisplay.GetTragetPoint(heartCount);
        }
        else
        {
            Debug.LogWarning($"[FloatingText] Invalid playerId: {playerId}");
            return;
        }

        // Safety check - use canvas as fallback if parent is null
        if (parent == null)
        {
            Debug.LogWarning($"[FloatingText] Could not get target point for player {playerId} (heartCount: {heartCount}), using canvas as parent");
            parent = canvasRectTransform;

            if (parent == null)
            {
                Debug.LogError("[FloatingText] canvasRectTransform is also null! Cannot show damage effect.");
                return;
            }
        }

        // Instantiate the UI text
        GameObject damageText = Instantiate(damageTextPrefab, canvasRectTransform);
        RectTransform damageRect = damageText.GetComponent<RectTransform>();

        if (damageRect == null)
        {
            Debug.LogError("[FloatingText] damageTextPrefab has no RectTransform component!");
            Destroy(damageText);
            return;
        }

        damageRect.anchoredPosition = localPoint;
        damageText.transform.SetParent(parent);

        // Start animation to target UI position
        StartCoroutine(AnimateCoinToTarget(damageRect, Vector2.zero));
    }

    private IEnumerator AnimateCoinToTarget(RectTransform heart, Vector2 targetPosition)
    {
        float duration = .1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * 1f;
            float t = elapsed / duration;
            heart.localScale = Vector3.Lerp(Vector3.one * 1.1f, Vector3.one * 0.5f , t);

            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        duration = 0.5f;
        elapsed = 0f;
        Vector2 startPosition = heart.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * 1f;
            float t = elapsed / duration;

            heart.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        Destroy(heart.gameObject);
    }

}
