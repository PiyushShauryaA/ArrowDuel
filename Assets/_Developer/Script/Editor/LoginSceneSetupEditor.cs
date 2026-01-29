using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class LoginSceneSetupEditor : EditorWindow
{
    private const string SCRIPT_PATH = "Assets/_Developer/Script/";
    private const string SCENE_PATH = "Assets/_Developer/_Scenes/";
    private const string LOGIN_SCENE_NAME = "LoginScene";
    private const string GAME_SCENE_NAME = "GameScene";

    [MenuItem("Tools/Setup Login Scene")]
    public static void ShowWindow()
    {
        GetWindow<LoginSceneSetupEditor>("Login Scene Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Login Scene Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Create Complete Login Scene Setup", GUILayout.Height(40)))
        {
            CreateLoginSceneSetup();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will:\n" +
            "1. Create LoginScene with UI\n" +
            "2. Add scenes to Build Settings\n" +
            "Note: Scripts (PlayerData.cs, LoginMenu.cs) are already created!", MessageType.Info);
    }

    [MenuItem("Tools/Setup Login Scene", true)]
    public static bool ValidateSetup()
    {
        return !EditorApplication.isPlaying;
    }

    private static void CreateLoginSceneSetup()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Setting Up Login Scene", "Creating LoginScene...", 0.3f);
            
            // Create LoginScene
            CreateLoginScene();
            
            EditorUtility.DisplayProgressBar("Setting Up Login Scene", "Updating Build Settings...", 0.8f);
            
            // Update Build Settings
            UpdateBuildSettings();
            
            EditorUtility.DisplayProgressBar("Setting Up Login Scene", "Refreshing assets...", 1.0f);
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            
            EditorUtility.ClearProgressBar();
            
            EditorUtility.DisplayDialog("Success!", 
                "Login Scene setup complete!\n\n" +
                "Created:\n" +
                "- LoginScene.unity\n\n" +
                "Updated:\n" +
                "- Build Settings\n\n" +
                "Open LoginScene to see the result!", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Error", $"Setup failed: {e.Message}", "OK");
            Debug.LogError($"Login Scene Setup Error: {e}");
        }
    }

    private static void CreateLoginScene()
    {
        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Remove default Main Camera and Directional Light (we'll add our own)
        GameObject[] rootObjects = newScene.GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            if (obj.name == "Main Camera" || obj.name == "Directional Light")
            {
                DestroyImmediate(obj);
            }
        }

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Create EventSystem
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Create LoginMenuManager GameObject
        GameObject loginManagerObj = new GameObject("LoginMenuManager");
        LoginMenu loginMenu = loginManagerObj.AddComponent<LoginMenu>();

        // Create Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Arrow Duel";
        titleText.fontSize = 72;
        titleText.alignment = TextAlignmentOptions.Center;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.8f);
        titleRect.anchorMax = new Vector2(0.5f, 0.8f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(600, 100);

        // Create Name Input Field
        GameObject inputObj = new GameObject("NameInputField");
        inputObj.transform.SetParent(canvasObj.transform, false);
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        
        // Input Field Background
        GameObject inputBackground = new GameObject("Background");
        inputBackground.transform.SetParent(inputObj.transform, false);
        UnityEngine.UI.Image bgImage = inputBackground.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform bgRect = inputBackground.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        inputField.targetGraphic = bgImage;

        // Input Field Text Area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);

        // Input Field Placeholder
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "Enter your name...";
        placeholderText.fontSize = 36;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        inputField.placeholder = placeholderText;

        // Input Field Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
        inputText.text = "";
        inputText.fontSize = 36;
        inputText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        inputField.textComponent = inputText;
        inputField.textViewport = textAreaRect;

        // Position Input Field
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.anchoredPosition = new Vector2(0, 50);
        inputRect.sizeDelta = new Vector2(600, 80);

        // Create Connect Button
        GameObject buttonObj = new GameObject("ConnectButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        Button button = buttonObj.AddComponent<Button>();
        
        // Button Background
        UnityEngine.UI.Image buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);
        button.targetGraphic = buttonImage;

        // Button Text
        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Connect";
        buttonText.fontSize = 42;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;

        // Position Button
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0, -80);
        buttonRect.sizeDelta = new Vector2(300, 80);

        // Create Play with AI Button
        GameObject aiButtonObj = new GameObject("PlayWithAIButton");
        aiButtonObj.transform.SetParent(canvasObj.transform, false);
        Button aiButton = aiButtonObj.AddComponent<Button>();
        
        // AI Button Background
        UnityEngine.UI.Image aiButtonImage = aiButtonObj.AddComponent<UnityEngine.UI.Image>();
        aiButtonImage.color = new Color(0.2f, 0.8f, 0.4f, 1f);
        aiButton.targetGraphic = aiButtonImage;

        // AI Button Text
        GameObject aiButtonTextObj = new GameObject("Text");
        aiButtonTextObj.transform.SetParent(aiButtonObj.transform, false);
        TextMeshProUGUI aiButtonText = aiButtonTextObj.AddComponent<TextMeshProUGUI>();
        aiButtonText.text = "Play with AI";
        aiButtonText.fontSize = 42;
        aiButtonText.alignment = TextAlignmentOptions.Center;
        aiButtonText.color = Color.white;
        RectTransform aiButtonTextRect = aiButtonTextObj.GetComponent<RectTransform>();
        aiButtonTextRect.anchorMin = Vector2.zero;
        aiButtonTextRect.anchorMax = Vector2.one;
        aiButtonTextRect.sizeDelta = Vector2.zero;

        // Position AI Button
        RectTransform aiButtonRect = aiButtonObj.GetComponent<RectTransform>();
        aiButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
        aiButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
        aiButtonRect.anchoredPosition = new Vector2(0, -180);
        aiButtonRect.sizeDelta = new Vector2(300, 80);

        // Create AI Difficulty Dropdown
        GameObject dropdownObj = new GameObject("AIDifficultyDropdown");
        dropdownObj.transform.SetParent(canvasObj.transform, false);
        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        
        // Dropdown Background
        GameObject dropdownBackground = new GameObject("Background");
        dropdownBackground.transform.SetParent(dropdownObj.transform, false);
        UnityEngine.UI.Image dropdownBgImage = dropdownBackground.AddComponent<UnityEngine.UI.Image>();
        dropdownBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform dropdownBgRect = dropdownBackground.GetComponent<RectTransform>();
        dropdownBgRect.anchorMin = Vector2.zero;
        dropdownBgRect.anchorMax = Vector2.one;
        dropdownBgRect.sizeDelta = Vector2.zero;
        dropdown.targetGraphic = dropdownBgImage;

        // Dropdown Label
        GameObject dropdownLabelObj = new GameObject("Label");
        dropdownLabelObj.transform.SetParent(dropdownObj.transform, false);
        TextMeshProUGUI dropdownLabel = dropdownLabelObj.AddComponent<TextMeshProUGUI>();
        dropdownLabel.text = "Normal";
        dropdownLabel.fontSize = 36;
        dropdownLabel.color = Color.white;
        RectTransform dropdownLabelRect = dropdownLabelObj.GetComponent<RectTransform>();
        dropdownLabelRect.anchorMin = new Vector2(0f, 0f);
        dropdownLabelRect.anchorMax = new Vector2(1f, 1f);
        dropdownLabelRect.offsetMin = new Vector2(10, 6);
        dropdownLabelRect.offsetMax = new Vector2(-25, -7);
        dropdown.captionText = dropdownLabel;

        // Dropdown Item Text Template
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(dropdownObj.transform, false);
        TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemLabel.fontSize = 36;
        itemLabel.color = Color.white;
        RectTransform itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = new Vector2(0f, 0f);
        itemLabelRect.anchorMax = new Vector2(1f, 1f);
        itemLabelRect.offsetMin = new Vector2(20, 0);
        itemLabelRect.offsetMax = new Vector2(-10, 0);
        dropdown.itemText = itemLabel;

        // Dropdown Template (for the dropdown list)
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(dropdownObj.transform, false);
        templateObj.SetActive(false);
        RectTransform templateRect = templateObj.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0, 2);
        templateRect.sizeDelta = new Vector2(0, 150);
        dropdown.template = templateRect;

        // Template Background
        GameObject templateBackground = new GameObject("Background");
        templateBackground.transform.SetParent(templateObj.transform, false);
        UnityEngine.UI.Image templateBgImage = templateBackground.AddComponent<UnityEngine.UI.Image>();
        templateBgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        RectTransform templateBgRect = templateBackground.GetComponent<RectTransform>();
        templateBgRect.anchorMin = Vector2.zero;
        templateBgRect.anchorMax = Vector2.one;
        templateBgRect.sizeDelta = Vector2.zero;

        // Template Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(templateObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        UnityEngine.UI.Mask viewportMask = viewportObj.AddComponent<UnityEngine.UI.Mask>();
        viewportMask.showMaskGraphic = false;
        UnityEngine.UI.Image viewportImage = viewportObj.AddComponent<UnityEngine.UI.Image>();
        viewportImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        // Template Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 28);
        contentRect.anchoredPosition = Vector2.zero;

        // Template Item
        GameObject itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);
        UnityEngine.UI.Toggle itemToggle = itemObj.AddComponent<UnityEngine.UI.Toggle>();
        itemToggle.isOn = false;
        RectTransform itemRect = itemObj.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 35);

        // Item Background
        GameObject itemBgObj = new GameObject("Item Background");
        itemBgObj.transform.SetParent(itemObj.transform, false);
        UnityEngine.UI.Image itemBgImage = itemBgObj.AddComponent<UnityEngine.UI.Image>();
        itemBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform itemBgRect = itemBgObj.GetComponent<RectTransform>();
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.sizeDelta = Vector2.zero;
        itemToggle.targetGraphic = itemBgImage;

        // Item Checkmark
        GameObject checkmarkObj = new GameObject("Item Checkmark");
        checkmarkObj.transform.SetParent(itemObj.transform, false);
        UnityEngine.UI.Image checkmarkImage = checkmarkObj.AddComponent<UnityEngine.UI.Image>();
        checkmarkImage.color = new Color(0.2f, 0.6f, 1f, 1f);
        RectTransform checkmarkRect = checkmarkObj.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(20, 20);
        checkmarkRect.anchoredPosition = new Vector2(10, 0);
        itemToggle.graphic = checkmarkImage;

        // Item Label (reuse the one created earlier)
        itemLabelObj.transform.SetParent(itemObj.transform, false);
        itemLabelRect.anchorMin = new Vector2(0f, 0f);
        itemLabelRect.anchorMax = new Vector2(1f, 1f);
        itemLabelRect.offsetMin = new Vector2(35, 1);
        itemLabelRect.offsetMax = new Vector2(-10, -2);

        // Position Dropdown
        RectTransform dropdownRect = dropdownObj.GetComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.5f, 0.5f);
        dropdownRect.anchorMax = new Vector2(0.5f, 0.5f);
        dropdownRect.anchoredPosition = new Vector2(0, -130);
        dropdownRect.sizeDelta = new Vector2(300, 60);

        // Set dropdown properties using SerializedObject (for protected properties)
        SerializedObject serializedDropdown = new SerializedObject(dropdown);
        serializedDropdown.FindProperty("m_Template").objectReferenceValue = templateRect;
        serializedDropdown.FindProperty("m_CaptionText").objectReferenceValue = dropdownLabel;
        serializedDropdown.FindProperty("m_ItemText").objectReferenceValue = itemLabel;
        if (serializedDropdown.FindProperty("m_Viewport") != null)
            serializedDropdown.FindProperty("m_Viewport").objectReferenceValue = viewportRect;
        if (serializedDropdown.FindProperty("m_Content") != null)
            serializedDropdown.FindProperty("m_Content").objectReferenceValue = contentRect;
        if (serializedDropdown.FindProperty("m_ItemTemplate") != null)
            serializedDropdown.FindProperty("m_ItemTemplate").objectReferenceValue = itemRect;
        serializedDropdown.ApplyModifiedProperties();

        // Create Error Text
        GameObject errorObj = new GameObject("ErrorText");
        errorObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI errorText = errorObj.AddComponent<TextMeshProUGUI>();
        errorText.text = "";
        errorText.fontSize = 28;
        errorText.alignment = TextAlignmentOptions.Center;
        errorText.color = Color.red;
        RectTransform errorRect = errorObj.GetComponent<RectTransform>();
        errorRect.anchorMin = new Vector2(0.5f, 0.5f);
        errorRect.anchorMax = new Vector2(0.5f, 0.5f);
        errorRect.anchoredPosition = new Vector2(0, -280);
        errorRect.sizeDelta = new Vector2(800, 50);
        errorObj.SetActive(false);

        // Assign references to LoginMenu
        SerializedObject serializedLoginMenu = new SerializedObject(loginMenu);
        serializedLoginMenu.FindProperty("nameInputField").objectReferenceValue = inputField;
        serializedLoginMenu.FindProperty("connectButton").objectReferenceValue = button;
        serializedLoginMenu.FindProperty("playWithAIButton").objectReferenceValue = aiButton;
        serializedLoginMenu.FindProperty("aiDifficultyDropdown").objectReferenceValue = dropdown;
        serializedLoginMenu.FindProperty("errorText").objectReferenceValue = errorText;
        serializedLoginMenu.FindProperty("gameSceneName").stringValue = GAME_SCENE_NAME;
        serializedLoginMenu.FindProperty("aiDifficulty").enumValueIndex = 1; // Normal difficulty
        serializedLoginMenu.ApplyModifiedProperties();

        // Save scene
        string scenePath = SCENE_PATH + LOGIN_SCENE_NAME + ".unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        //Debug.Log($"Created scene: {scenePath}");
    }

    private static void UpdateBuildSettings()
    {
        // Get current scenes
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        
        // Check if LoginScene already exists
        bool loginSceneExists = false;
        bool gameSceneExists = false;
        
        foreach (var scene in scenes)
        {
            if (scene.path.Contains(LOGIN_SCENE_NAME + ".unity"))
            {
                loginSceneExists = true;
            }
            if (scene.path.Contains(GAME_SCENE_NAME + ".unity"))
            {
                gameSceneExists = true;
            }
        }

        // Add LoginScene if not exists
        if (!loginSceneExists)
        {
            string loginScenePath = SCENE_PATH + LOGIN_SCENE_NAME + ".unity";
            if (File.Exists(loginScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(loginScenePath, true));
                //Debug.Log($"Added {LOGIN_SCENE_NAME} to Build Settings");
            }
        }

        // Ensure GameScene exists
        if (!gameSceneExists)
        {
            string gameScenePath = SCENE_PATH + GAME_SCENE_NAME + ".unity";
            if (File.Exists(gameScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(gameScenePath, true));
                //Debug.Log($"Added {GAME_SCENE_NAME} to Build Settings");
            }
        }

        // Apply changes
        EditorBuildSettings.scenes = scenes.ToArray();
        //Debug.Log("Build Settings updated");
    }
}
