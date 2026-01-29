using System.Collections;
using UnityEngine;

public class KauaaSpawner : MonoBehaviour
{
    public GameObject kauaaPrefab;       // assign your Kauaa prefab here
    public Transform kauaaSpawnPoint;    // assign your kauaa_spawnPoint here
    public float spawnInterval = 60f;    // one minute

    private Coroutine spawnRoutine;

    private void OnEnable()
    {
        // Optional: start only when gameplay starts
        GameManager.onGameStart += OnGameStart;
    }

    private void OnDisable()
    {
        GameManager.onGameStart -= OnGameStart;
    }

    private void OnGameStart()
    {
        if (spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            // Wait 1 minute
            yield return new WaitForSeconds(spawnInterval);

            // Only spawn during gameplay
            if (GameManager.instance != null &&
                GameManager.instance.gameState == GameState.Gameplay)
            {
                SpawnKauaa();
            }
        }
    }

    private void SpawnKauaa()
    {
        if (kauaaPrefab == null || kauaaSpawnPoint == null) return;

        GameObject kauaa = Instantiate(kauaaPrefab, kauaaSpawnPoint.position, kauaaSpawnPoint.rotation);

        // Ensure LoopMovementObject is set up correctly
        LoopMovementObject loopMovement = kauaa.GetComponent<LoopMovementObject>();
        if (loopMovement != null)
        {
            loopMovement.isBird = true;
            loopMovement.directionType = LoopMovementObject.DirectionType.Left;
            loopMovement.canMove = true; // Start moving immediately
        }
    }
}