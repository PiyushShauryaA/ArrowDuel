using UnityEngine;

public class Kauaa : MonoBehaviour
{
    public float speed = 5f;      // movement speed
    public float lifeTime = 20f;  // destroy after some time (optional)

    private void Start()
    {
        // Auto-destroy so it doesn't live forever
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Move from right to left in world space
        transform.Translate(Vector2.left * speed * Time.deltaTime, Space.World);
    }
}