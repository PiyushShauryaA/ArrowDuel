using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BirdManager : MonoBehaviour
{
    public static BirdManager instance;

    [SerializeField] private GameObject[] birdPrefabs;
    [SerializeField] private Transform[] spawnPoints;



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
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && 
            (NakamaNetworkManager.Instance != null && !NakamaNetworkManager.Instance.HasStateAuthorityGameData))
        {
            Debug.LogWarning("InitBirds called without authority. Exiting.");
            return;
        }

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null && spawnPoint.gameObject.activeInHierarchy)
            {
                validSpawnPoints.Add(spawnPoint);
            }
        }

        SpawnBird();

    }

    async void SpawnBird()
    {

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            int birdIndex = Random.Range(0, birdPrefabs.Length);
            int spawnIndex = Random.Range(0, validSpawnPoints.Count);

            GameObject bird;
            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            {
                var birdClone = Instantiate(birdPrefabs[birdIndex], validSpawnPoints[spawnIndex].position, Quaternion.identity);
                bird = birdClone;
                OnBirdLoad(bird);
            }
            else
            {
                bool hasAuthority = NakamaNetworkManager.Instance != null && NakamaNetworkManager.Instance.HasStateAuthorityGameData;
                if (hasAuthority)
                {
                    // Use regular Instantiate for Nakama (no network spawning needed)
                    var birdClone = Instantiate(birdPrefabs[birdIndex], validSpawnPoints[spawnIndex].position, Quaternion.identity);
                    bird = birdClone;
                    OnBirdLoad(bird);
                }
            }


            validSpawnPoints.RemoveAt(spawnIndex);
        }

    }

    private void OnBirdLoad(GameObject bird)
    {
        //Debug.Log($"OnBirdLoad");

        LoopMovementObject movementObject = bird.GetComponent<LoopMovementObject>();

        if (bird.transform.position.x < 0)
        {
            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            {
                bird.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                // No NetworkTransform needed for Nakama - use regular transform
                bird.transform.position = bird.transform.position;
                bird.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            movementObject.directionType = LoopMovementObject.DirectionType.Left;
        }
        else
        {
            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            {
                bird.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                // No NetworkTransform needed for Nakama - use regular transform
                bird.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            movementObject.directionType = LoopMovementObject.DirectionType.Left;
        }

        movementObject.delayMoveStart = Random.Range(0f, 3f);

    }


}
