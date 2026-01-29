using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindIconWobble : MonoBehaviour
{
    public float wobbleAmount = 0.5f;
    public float wobbleSpeed = 2f;
    public float rotationAmount = 5f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;
            
        // Wobble effect
        float wobble = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount;
        transform.localPosition = startPosition + new Vector3(wobble, 0, 0);

        // Slight rotation
        float rotation = Mathf.Sin(Time.time * wobbleSpeed * 0.7f) * rotationAmount;
        transform.localRotation = startRotation * Quaternion.Euler(0, 0, rotation);
    }
}
