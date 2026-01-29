using UnityEngine;
using TMPro;

/// <summary>
/// Displays rotation status for both players on screen for testing purposes.
/// Attach this to a GameObject in the scene and assign UI Text components.
/// </summary>
public class RotationTestDisplay : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    
    [Header("UI Text")]
    public TextMeshProUGUI player1StatusText;
    public TextMeshProUGUI player2StatusText;
    
    void Update()
    {
        if (gameManager == null) return;
        
        // Player 1 (Left) Status
        if (gameManager.playerController != null && player1StatusText != null)
        {
            var localSync = gameManager.playerController.GetComponent<PlayerNetworkLocalSync>();
            var remoteSync = gameManager.playerController.GetComponent<PlayerNetworkRemoteSync>();
            
            string syncType = localSync != null ? "LOCAL" : (remoteSync != null ? "REMOTE" : "NONE");
            float rotation = gameManager.playerController.bowParent != null 
                ? gameManager.playerController.bowParent.rotation.eulerAngles.z 
                : 0f;
            float autoAngle = gameManager.playerController.currentAutoRotationAngle;
            bool rotationEnabled = remoteSync != null ? remoteSync.rotationEnabled : true;
            
            player1StatusText.text = $"Player 1 (Left)\n" +
                $"Type: {syncType}\n" +
                $"Rotation: {rotation:F1}°\n" +
                $"AutoAngle: {autoAngle:F1}°\n" +
                $"Enabled: {rotationEnabled}\n" +
                $"Charging: {gameManager.playerController.isCharging}";
        }
        
        // Player 2 (Right) Status
        if (gameManager.opponentPlayerController != null && player2StatusText != null)
        {
            var localSync = gameManager.opponentPlayerController.GetComponent<PlayerNetworkLocalSync>();
            var remoteSync = gameManager.opponentPlayerController.GetComponent<PlayerNetworkRemoteSync>();
            
            string syncType = localSync != null ? "LOCAL" : (remoteSync != null ? "REMOTE" : "NONE");
            float rotation = gameManager.opponentPlayerController.bowParent != null 
                ? gameManager.opponentPlayerController.bowParent.rotation.eulerAngles.z 
                : 0f;
            float autoAngle = gameManager.opponentPlayerController.currentAutoRotationAngle;
            bool rotationEnabled = remoteSync != null ? remoteSync.rotationEnabled : true;
            
            player2StatusText.text = $"Player 2 (Right)\n" +
                $"Type: {syncType}\n" +
                $"Rotation: {rotation:F1}°\n" +
                $"AutoAngle: {autoAngle:F1}°\n" +
                $"Enabled: {rotationEnabled}\n" +
                $"Charging: {gameManager.opponentPlayerController.isCharging}";
        }
    }
}
