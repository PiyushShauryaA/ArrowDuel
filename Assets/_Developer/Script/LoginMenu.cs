using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class LoginMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nameInputField;
   // [SerializeField] private Button connectButton;
    [SerializeField] private Button playWithAIButton;
    [SerializeField] private TMP_Dropdown aiDifficultyDropdown;
    [SerializeField] private TextMeshProUGUI errorText;
    
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private int minNameLength = 2;
    [SerializeField] private int maxNameLength = 20;
    
    [Header("AI Settings")]
    [SerializeField] private AiSkillLevels aiDifficulty = AiSkillLevels.Normal;

    private void Start()
    {
        // Set up button listeners
        /*if (connectButton != null)
        {
           // connectButton.onClick.AddListener(OnConnectButtonClicked);
        }*/

        if (playWithAIButton != null)
        {
            playWithAIButton.onClick.AddListener(OnPlayWithAIClicked);
        }

        // Set up AI difficulty dropdown
        SetupAIDifficultyDropdown();

        // Enable Enter key to submit
        

        // Load saved name if exists
        LoadSavedName();

        // Hide error text initially
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }
    }

    private void SetupAIDifficultyDropdown()
    {
        if (aiDifficultyDropdown == null)
            return;

        // Clear existing options
        aiDifficultyDropdown.ClearOptions();

        // Create options list from AiSkillLevels enum
        var options = new System.Collections.Generic.List<string>();
        options.Add("Easy");
        options.Add("Normal");
        options.Add("Hard");
        options.Add("Robinhood");

        aiDifficultyDropdown.AddOptions(options);

        // Set default selection based on current aiDifficulty
        aiDifficultyDropdown.value = (int)aiDifficulty;

        // Add listener for when dropdown value changes
        aiDifficultyDropdown.onValueChanged.AddListener(OnAIDifficultyChanged);
    }

    private void OnAIDifficultyChanged(int selectedIndex)
    {
        // Update aiDifficulty based on selected dropdown index
        aiDifficulty = (AiSkillLevels)selectedIndex;
        //Debug.Log($"[LoginMenu] AI Difficulty changed to: {aiDifficulty}");
    }

    private void LoadSavedName()
    {
        if (nameInputField != null)
        {
            string savedName = PlayerPrefs.GetString("PlayerName", "");
            if (!string.IsNullOrEmpty(savedName))
            {
                nameInputField.text = savedName;
            }
        }
    }

    private void OnNameSubmitted(string name)
    {
        OnConnectButtonClicked();
    }

    public void OnConnectButtonClicked()
    {
        //Debug.Log("[LoginMenu] === CONNECT BUTTON CLICKED ===");
        
        if (!ValidateName())
        {
            Debug.LogWarning("[LoginMenu] Name validation failed!");
            return;
        }

        //Debug.Log($"[LoginMenu] PlayerName: {PlayerData.playerName}");
        
        // Set game mode to multiplayer
        PlayerData.gameMode = GameModeType.MULTIPLAYER;
        PlayerData.isAIMode = false;
        
        //Debug.Log("[LoginMenu] GameMode set to MULTIPLAYER");
        //Debug.Log("[LoginMenu] Loading GameScene...");

        // Load game scene
        LoadGameScene();
    }

    public void OnPlayWithAIClicked()
    {
        if (!ValidateName())
            return;

        // Set game mode to singleplayer with AI
        PlayerData.gameMode = GameModeType.SINGLEPLAYER;
        PlayerData.isAIMode = true;
        PlayerData.aiDifficulty = aiDifficulty;

        // Load game scene
        LoadGameScene();
    }

    private bool ValidateName()
    {
        string playerName = nameInputField != null ? nameInputField.text.Trim() : "";

        // Validate name
        if (string.IsNullOrEmpty(playerName))
        {
            ShowError("Please enter your name!");
            return false;
        }

        if (playerName.Length < minNameLength)
        {
            ShowError($"Name must be at least {minNameLength} characters!");
            return false;
        }

        if (playerName.Length > maxNameLength)
        {
            ShowError($"Name must be less than {maxNameLength} characters!");
            return false;
        }

        // Save player name
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        // Store in static class for access in game
        PlayerData.playerName = playerName;
        ArrowduelConnectionManager.Instance.playerName = playerName;

        // Hide error if validation passed
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }

        return true;
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
        Debug.LogWarning($"[LoginMenu] {message}");
    }

    private void LoadGameScene()
    {
        //Debug.Log($"[LoginMenu] Loading game scene: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnDestroy()
    {
        // Clean up listeners
        /*if (connectButton != null)
        {
            connectButton.onClick.RemoveAllListeners();
        }*/
        if (playWithAIButton != null)
        {
            playWithAIButton.onClick.RemoveAllListeners();
        }
        if (aiDifficultyDropdown != null)
        {
            aiDifficultyDropdown.onValueChanged.RemoveAllListeners();
        }
        if (nameInputField != null)
        {
            nameInputField.onSubmit.RemoveAllListeners();
        }
    }
}
