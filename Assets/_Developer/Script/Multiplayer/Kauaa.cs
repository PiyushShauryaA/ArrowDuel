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
       // Debug.Log("Kauaa Update running"); // Add this line
        transform.Translate(Vector2.left * speed * Time.deltaTime, Space.World);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
       // Debug.Log($"Kauaa OnTriggerEnter2D called! Collision: {collision.gameObject.name} {collision.gameObject.tag}");
        if (collision.gameObject.tag=="arrow")
        {
            //Destroy(this.gameObject, 0.2f);
            collision.gameObject.GetComponent<Arrow>().BirdHitEffect(collision.transform);
            Destroy(this.gameObject, 0.2f);
        }

        
    }
}