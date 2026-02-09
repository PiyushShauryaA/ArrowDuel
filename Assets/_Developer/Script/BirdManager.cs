using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BirdManager : MonoBehaviour
{
    public static BirdManager instance;

    [SerializeField] private GameObject[] birdPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    public Vector3 direction;  // Change from private to public

    private void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        GameManager.onGameStart += InitBirds;

    }

    void OnDisable()
    {
        GameManager.onGameStart -= InitBirds;

    }

    List<Transform> validSpawnPoints = new List<Transform>();

    public void InitBirds()
    {
        validSpawnPoints.Clear();

        // Remove authority check - let both players spawn birds locally

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null && spawnPoint.gameObject.activeInHierarchy)
            {
                validSpawnPoints.Add(spawnPoint);
            }
        }

        SpawnBird();
    }

    void SpawnBird()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            int birdIndex = Random.Range(0, birdPrefabs.Length);
            int spawnIndex = Random.Range(0, validSpawnPoints.Count);

            // Spawn for both singleplayer AND multiplayer
            var birdClone = Instantiate(birdPrefabs[birdIndex], validSpawnPoints[spawnIndex].position, Quaternion.identity);
            OnBirdLoad(birdClone);

            if (validSpawnPoints.Count > 0)
                validSpawnPoints.RemoveAt(spawnIndex);
        }
    }

    private void OnBirdLoad(GameObject bird)
    {
        LoopMovementObject movementObject = bird.GetComponent<LoopMovementObject>();

        movementObject.isBird = true;

        if (bird.transform.position.x < 0)
        {
            bird.transform.rotation = Quaternion.Euler(0, 180, 0);
            movementObject.directionType = LoopMovementObject.DirectionType.Right;
            movementObject.SetDirection(Vector3.right);  // ADD THIS - or directly set if public
        }
        else
        {
            bird.transform.rotation = Quaternion.Euler(0, 0, 0);
            movementObject.directionType = LoopMovementObject.DirectionType.Left;
            movementObject.SetDirection(Vector3.left);   // ADD THIS - or directly set if public
        }

        movementObject.delayMoveStart = Random.Range(0f, 3f);
        movementObject.canMove = true;
    }
    

}
