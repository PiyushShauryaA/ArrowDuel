using UnityEngine;
using UnityEngine.UI;

public class HeartDisplay : MonoBehaviour
{

    public int playerNumber = 1;

    [Space(05)]
    [SerializeField] private Sprite fullHeart, halfHeart, emptyHeart;

    [Space(05)]
    [SerializeField] private Image[] heartImages; // 5 heart containers

    public void UpdateHearts(float currentHearts)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            float heartStatus = currentHearts - i;
            ////Debug.Log($"heartStatus: {heartStatus}");

            if (heartStatus >= 1f)
            {
                // Full heart
                heartImages[i].sprite = fullHeart;
            }
            else if (heartStatus > 0f)
            {
                // Half heart
                heartImages[i].sprite = halfHeart;
            }
            else
            {
                // Empty heart
                heartImages[i].sprite = emptyHeart;
            }
        }
    }

    public RectTransform GetTragetPoint(int index)
    {
        // Validate and clamp index to valid range (1 to heartImages.Length)
        // Index is 1-based (1 = first heart, 2 = second heart, etc.)
        if (heartImages == null || heartImages.Length == 0)
        {
            Debug.LogError("[HeartDisplay] heartImages array is null or empty!");
            return null;
        }

        // Clamp index to valid range: minimum 1, maximum heartImages.Length
        int clampedIndex = Mathf.Clamp(index, 1, heartImages.Length);

        // Convert to 0-based array index
        int arrayIndex = clampedIndex - 1;

        // Final safety check
        if (arrayIndex < 0 || arrayIndex >= heartImages.Length)
        {
            Debug.LogWarning($"[HeartDisplay] Invalid index {index}, using first heart. Array length: {heartImages.Length}");
            arrayIndex = 0;
        }

        // Check if the heart image exists
        if (heartImages[arrayIndex] == null)
        {
            Debug.LogError($"[HeartDisplay] Heart image at index {arrayIndex} is null!");
            return null;
        }

        RectTransform rectTransform = heartImages[arrayIndex].GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"[HeartDisplay] Heart image at index {arrayIndex} has no RectTransform component!");
            return null;
        }

        return rectTransform;
    }

}
