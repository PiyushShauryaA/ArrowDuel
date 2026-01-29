using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{

    public string bombPlayerTag;
    public bool isPlayerBomb;
    public LayerMask layerMask;

    public float blastRadius = 5f;

    public bool HasStateAuthority;

    private void OnEnable()
    {
        Explode();
    }

    public void Explode()
    {
        // Check for players in blast radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, blastRadius, layerMask);

        foreach (Collider2D hit in colliders)
        {
            BowController player = hit.GetComponent<BowController>();

            if (player == null)
                return;

            if (GameManager.gameMode == GameModeType.MULTIPLAYER)
                //Debug.Log($"TAG: {player.tag} | {bombPlayerTag} | {isPlayerBomb} ||| {HasStateAuthority}");

            if (player.tag == bombPlayerTag)
            {
                isPlayerBomb = false;
            }

            player.GetComponent<FloatingText>().ShowDamageEffect();
            GameObject heartBreakEffect = player.heartBreakEffect;

            if (heartBreakEffect.activeInHierarchy)
                heartBreakEffect.SetActive(false);

            heartBreakEffect.SetActive(true);

            // GameManager.onHitTarget?.Invoke(isPlayerBomb);
            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
                GameManager.onHitTarget?.Invoke(isPlayerBomb);
            else if (GameManager.gameMode == GameModeType.MULTIPLAYER && HasStateAuthority)
            {
                //Debug.Log($"TAG: HIT_TARGET RPC CALL FOR ALL");
                if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
                {
                    const long OPCODE_HIT_TARGET = 7;
                    var data = new HitTargetData { isPlayerArrow = isPlayerBomb };
                    string json = JsonUtility.ToJson(data);
                    ArrowduelNakamaClient.Instance.SendMatchStateAsync(OPCODE_HIT_TARGET, json);
                }
                // GameManager.onHitTarget?.Invoke(isPlayerArrowNetwork);
            }

        }

    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }


}
