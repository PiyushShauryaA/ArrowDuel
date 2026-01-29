using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPowerUp : MonoBehaviour
{

    [SerializeField] private BowController controller;

    public PowerUpType playerPowerUpType = PowerUpType.None;
    public bool isShieldActive = false;
    public bool isFrozen = false;

    [Header("Effect / Power ")]
    public Color freezeColor = Color.cyan;
    public SpriteRenderer[] sprites;
    public GameObject freezePowerObj;
    public GameObject shieldPowerObj;
    public GameObject bombPowerObj;


    private void Start()
    {
        // controller = GetComponent<BowController>();
        GameManager.onGameLevelChange += OnGameLevelChange;

    }

    private void OnDestroy()
    {
        GameManager.onGameLevelChange -= OnGameLevelChange;

    }

    public void CollectPowerUp(PowerUpType powerType)
    {

        // powerUpDisplayImage.gameObject.SetActive(false);
        playerPowerUpType = powerType;
        // //Debug.Log($"Collected {powerType} power!!!!!!", this);

        // ShowPowerUpDisplay();
        IsOpponentFreezePower();

    }

    public void ResetPower()
    {
        playerPowerUpType = PowerUpType.None;

    }


    #region SHIELD POWER :

    public bool IsShieldPowerActive()
    {
        if (playerPowerUpType == PowerUpType.Shield)
        {
            //Debug.Log($"<color=yellow> OPPONENT HAS <b>SHIELD</b> POWER...! </color>");
            ActivateShield();
            return true;
        }

        return false;
    }

    private void ActivateShield()
    {
        ResetPower();
        isShieldActive = true;
        shieldPowerObj.SetActive(true);
        Invoke(nameof(DeactivateShield), 1f);
    }

    private void DeactivateShield()
    {
        isShieldActive = false;
        shieldPowerObj.SetActive(false);
    }

    #endregion


    #region FREEZE POWER :

    private void IsOpponentFreezePower()
    {

        if (playerPowerUpType == PowerUpType.Freeze)
        {
            if (GameManager.instance.playerController.playerID != controller.playerID)
            {
                GameManager.instance.playerController.playerPowerUp.FreezeOpponent();
                ResetPower();

            }
            else if (GameManager.instance.opponentPlayerController.playerID != controller.playerID)
            {
                GameManager.instance.opponentPlayerController.playerPowerUp.FreezeOpponent();
                ResetPower();

            }

        }
    }

    private void FreezeOpponent()
    {

        isFrozen = true;
        // //Debug.Log($"power >>>", this);
        // controller.bowParent.gameObject.SetActive(false);
        // freezePowerObj.SetActive(true);
        foreach (var sprite in sprites)
        {
            sprite.color = freezeColor;
        }
        Invoke(nameof(UnfreezeOpponent), PowerUpManager.instance.freezeDuration);
    }

    private void UnfreezeOpponent()
    {

        isFrozen = false;
        // controller.bowParent.gameObject.SetActive(true);
        foreach (var sprite in sprites)
        {
            sprite.color = Color.white;
        }
        // freezePowerObj.SetActive(false);
    }

    #endregion


    #region BOMB POWER :


    public void OnBombActive()
    {
        ResetPower();
        // PowerUpManager.instance.bombBlastEffect.transform.position = targetTransform.position;
        // PowerUpManager.instance.bombBlastEffect.SetActive(true);

        // Invoke(nameof(DeactivateBomb), .5f);
    }

    private void DeactivateBomb()
    {
        // PowerUpManager.instance.bombBlastEffect.SetActive(false);
        // bombPowerObj.SetActive(false);
    }

    #endregion


    #region BIG_ARROW POWER :

    public void DeactiveBigArrowPower()
    {
        ResetPower();
    }

    #endregion


    public void OnGameLevelChange()
    {
        ResetPower();

    }

}
