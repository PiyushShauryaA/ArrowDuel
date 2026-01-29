using UnityEngine;

public class PowerUp : MonoBehaviour
{

    private PowerUpType powerType;
    public SpriteRenderer powerSprite;

    [SerializeField] private float floatHeight = 0.2f;
    [SerializeField] private float floatSpeed = 1f;
    private Vector3 startPos;

    private void Start()
    {
        startPos = powerSprite.transform.position;
    }

    private void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        powerSprite.transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    public void Initialize(PowerUpType type, Sprite powerSprite)
    {
        powerType = type;
        this.powerSprite.sprite = powerSprite;
    }

    public void Collect(BowController controller)
    {
        PowerUpManager.instance.CollectPowerUp(powerType, controller);
        Destroy(gameObject, 0.1f);
    }

}
