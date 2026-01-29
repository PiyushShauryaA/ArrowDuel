using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Editor script to automatically fix all multiplayer-related issues in scenes and prefabs.
/// Assigns all references, adds missing components, and validates setup.
/// </summary>
public class MultiplayerSceneFixer : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showDetails = true;
    private List<string> fixLog = new List<string>();

    [MenuItem("Tools/Multiplayer Scene Fixer")]
    public static void ShowWindow()
    {
        GetWindow<MultiplayerSceneFixer>("Multiplayer Scene Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Multiplayer Scene & Prefab Fixer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "• Fix GameManager references in scenes\n" +
            "• Assign player prefabs automatically\n" +
            "• Fix ArrowduelConnectionManager UI references\n" +
            "• Add missing network sync components to prefabs\n" +
            "• Validate and fix all multiplayer setup\n" +
            "• Auto-assign all missing references",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Fix All Scenes", GUILayout.Height(40)))
        {
            FixAllScenes();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Fix All Prefabs", GUILayout.Height(40)))
        {
            FixAllPrefabs();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Fix Everything (Scenes + Prefabs)", GUILayout.Height(50)))
        {
            FixEverything();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        showDetails = EditorGUILayout.Foldout(showDetails, "Fix Log", true);
        if (showDetails && fixLog.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (var log in fixLog)
            {
                EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void FixEverything()
    {
        fixLog.Clear();
        AddLog("=== Starting Complete Fix ===");

        try
        {
            EditorUtility.DisplayProgressBar("Fixing Everything", "Processing...", 0f);

            // Fix prefabs first
            AddLog("\n--- Fixing Prefabs ---");
            FixAllPrefabs();

            // Then fix scenes
            AddLog("\n--- Fixing Scenes ---");
            FixAllScenes();

            EditorUtility.ClearProgressBar();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success!",
                $"Fixed all scenes and prefabs!\n\n" +
                $"Check the log below for details.",
                "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Error", $"Fix failed: {e.Message}", "OK");
            Debug.LogError($"Multiplayer Scene Fixer Error: {e}");
        }
    }

    private void FixAllScenes()
    {
        fixLog.Clear();
        AddLog("=== Fixing All Scenes ===");

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Developer/_Scenes" });
        int totalScenes = sceneGuids.Length;
        int currentScene = 0;

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            currentScene++;
            EditorUtility.DisplayProgressBar("Fixing Scenes", $"Processing {System.IO.Path.GetFileName(scenePath)}...", 
                (float)currentScene / totalScenes);

            AddLog($"\n--- Fixing Scene: {System.IO.Path.GetFileName(scenePath)} ---");
            FixScene(scenePath);
        }

        EditorUtility.ClearProgressBar();
        AddLog("\n=== Scene Fix Complete ===");
    }

    private void FixScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        bool sceneModified = false;

        // Fix GameManager
        GameManager gameManager = Object.FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            AddLog("Found GameManager, fixing references...");
            sceneModified |= FixGameManager(gameManager);
        }
        else
        {
            AddLog("Warning: GameManager not found in scene");
        }

        // Fix ArrowduelConnectionManager
        ArrowduelConnectionManager connectionManager = Object.FindObjectOfType<ArrowduelConnectionManager>();
        if (connectionManager != null)
        {
            AddLog("Found ArrowduelConnectionManager, fixing references...");
            sceneModified |= FixConnectionManager(connectionManager);
        }

        // Fix ArrowduelNetworkManager
        ArrowduelNetworkManager networkManager = Object.FindObjectOfType<ArrowduelNetworkManager>();
        if (networkManager == null)
        {
            AddLog("Creating ArrowduelNetworkManager...");
            GameObject networkManagerObj = new GameObject("ArrowduelNetworkManager");
            networkManagerObj.AddComponent<ArrowduelNetworkManager>();
            sceneModified = true;
        }

        // Ensure UnityMainThreadDispatcher exists
        UnityMainThreadDispatcher dispatcher = Object.FindObjectOfType<UnityMainThreadDispatcher>();
        if (dispatcher == null)
        {
            AddLog("Creating UnityMainThreadDispatcher...");
            GameObject dispatcherObj = new GameObject("UnityMainThreadDispatcher");
            dispatcherObj.AddComponent<UnityMainThreadDispatcher>();
            sceneModified = true;
        }

        if (sceneModified)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddLog("✓ Scene saved");
        }
        else
        {
            AddLog("✓ Scene already fixed");
        }
    }

    private bool FixGameManager(GameManager gameManager)
    {
        bool modified = false;

        // Find and assign player prefabs
        if (gameManager.player1NetworkStatePrefab == null)
        {
            GameObject player1Prefab = FindPrefabByName("Player Prefab", "Player_v3 Prefab", "Player");
            if (player1Prefab != null)
            {
                // Verify it has PlayerController
                if (player1Prefab.GetComponent<PlayerController>() != null || 
                    player1Prefab.GetComponentInChildren<PlayerController>() != null)
                {
                    gameManager.player1NetworkStatePrefab = player1Prefab;
                    AddLog($"✓ Assigned player1NetworkStatePrefab: {player1Prefab.name}");
                    modified = true;
                }
                else
                {
                    AddLog($"⚠ Found prefab {player1Prefab.name} but it doesn't have PlayerController");
                }
            }
            else
            {
                AddLog("✗ Could not find Player 1 prefab");
            }
        }
        else
        {
            AddLog($"✓ player1NetworkStatePrefab already assigned: {gameManager.player1NetworkStatePrefab.name}");
        }

        if (gameManager.player2NetworkStatePrefab == null)
        {
            GameObject player2Prefab = FindPrefabByName("Opponent Prefab", "Opponent_v3 Prefab", "Opponent");
            if (player2Prefab != null)
            {
                // Verify it has OpponentController
                if (player2Prefab.GetComponent<OpponentController>() != null || 
                    player2Prefab.GetComponentInChildren<OpponentController>() != null)
                {
                    gameManager.player2NetworkStatePrefab = player2Prefab;
                    AddLog($"✓ Assigned player2NetworkStatePrefab: {player2Prefab.name}");
                    modified = true;
                }
                else
                {
                    AddLog($"⚠ Found prefab {player2Prefab.name} but it doesn't have OpponentController");
                }
            }
            else
            {
                AddLog("✗ Could not find Player 2 prefab");
            }
        }
        else
        {
            AddLog($"✓ player2NetworkStatePrefab already assigned: {gameManager.player2NetworkStatePrefab.name}");
        }

        // Find and assign UI references using SerializedObject (for private fields)
        SerializedObject serializedGameManager = new SerializedObject(gameManager);

        // Find player1NameText
        SerializedProperty player1NameTextProp = serializedGameManager.FindProperty("player1NameText");
        if (player1NameTextProp != null && player1NameTextProp.objectReferenceValue == null)
        {
            TextMeshProUGUI[] texts = Object.FindObjectsOfType<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Player1") || text.name.Contains("Player 1") || text.name.Contains("P1"))
                {
                    player1NameTextProp.objectReferenceValue = text;
                    AddLog($"✓ Assigned player1NameText: {text.name}");
                    modified = true;
                    break;
                }
            }
        }

        // Find player2NameText
        SerializedProperty player2NameTextProp = serializedGameManager.FindProperty("player2NameText");
        if (player2NameTextProp != null && player2NameTextProp.objectReferenceValue == null)
        {
            TextMeshProUGUI[] texts = Object.FindObjectsOfType<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Player2") || text.name.Contains("Player 2") || text.name.Contains("P2"))
                {
                    player2NameTextProp.objectReferenceValue = text;
                    AddLog($"✓ Assigned player2NameText: {text.name}");
                    modified = true;
                    break;
                }
            }
        }

        // Find waitingText
        SerializedProperty waitingTextProp = serializedGameManager.FindProperty("waitingText");
        if (waitingTextProp != null && waitingTextProp.objectReferenceValue == null)
        {
            TextMeshProUGUI[] texts = Object.FindObjectsOfType<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Waiting") || text.name.Contains("waiting"))
                {
                    waitingTextProp.objectReferenceValue = text;
                    AddLog($"✓ Assigned waitingText: {text.name}");
                    modified = true;
                    break;
                }
            }
        }

        serializedGameManager.ApplyModifiedProperties();

        // Find waiting panel
        if (gameManager.waitingPanel == null)
        {
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Waiting") || obj.name.Contains("waiting"))
                {
                    gameManager.waitingPanel = obj;
                    AddLog($"✓ Assigned waitingPanel: {obj.name}");
                    modified = true;
                    break;
                }
            }
        }

        // Find wind indicators
        if (gameManager.windIndicators == null || gameManager.windIndicators.Length == 0)
        {
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            List<GameObject> windIndicators = new List<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Wind") || obj.name.Contains("wind"))
                {
                    windIndicators.Add(obj);
                }
            }
            if (windIndicators.Count > 0)
            {
                gameManager.windIndicators = windIndicators.ToArray();
                AddLog($"✓ Assigned {windIndicators.Count} wind indicators");
                modified = true;
            }
        }

        // Find arrow hit effect
        if (gameManager.arrowHitEffect == null)
        {
            GameObject hitEffect = FindPrefabByName("Hit", "Effect", "ArrowHit");
            if (hitEffect != null)
            {
                gameManager.arrowHitEffect = hitEffect;
                AddLog($"✓ Assigned arrowHitEffect: {hitEffect.name}");
                modified = true;
            }
        }

        return modified;
    }

    private bool FixConnectionManager(ArrowduelConnectionManager connectionManager)
    {
        bool modified = false;

        // Find and assign UI references using SerializedObject
        SerializedObject serializedObject = new SerializedObject(connectionManager);

        // Find connect button
        SerializedProperty connectButtonProp = serializedObject.FindProperty("connectButton");
        if (connectButtonProp != null && connectButtonProp.objectReferenceValue == null)
        {
            Button[] buttons = Object.FindObjectsOfType<Button>();
            foreach (var button in buttons)
            {
                if (button.name.Contains("Connect") || button.name.Contains("connect"))
                {
                    connectButtonProp.objectReferenceValue = button;
                    AddLog($"✓ Assigned connectButton: {button.name}");
                    modified = true;
                    break;
                }
            }
        }

        // Find status text
        SerializedProperty statusTextProp = serializedObject.FindProperty("statusText");
        if (statusTextProp != null && statusTextProp.objectReferenceValue == null)
        {
            TextMeshProUGUI[] texts = Object.FindObjectsOfType<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Status") || text.name.Contains("status"))
                {
                    statusTextProp.objectReferenceValue = text;
                    AddLog($"✓ Assigned statusText: {text.name}");
                    modified = true;
                    break;
                }
            }
        }

        // Find username input
        SerializedProperty usernameInputProp = serializedObject.FindProperty("usernameInput");
        if (usernameInputProp != null && usernameInputProp.objectReferenceValue == null)
        {
            TMP_InputField[] inputs = Object.FindObjectsOfType<TMP_InputField>();
            foreach (var input in inputs)
            {
                if (input.name.Contains("Username") || input.name.Contains("Name") || input.name.Contains("username") || input.name.Contains("name"))
                {
                    usernameInputProp.objectReferenceValue = input;
                    AddLog($"✓ Assigned usernameInput: {input.name}");
                    modified = true;
                    break;
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
        return modified;
    }

    private void FixAllPrefabs()
    {
        fixLog.Clear();
        AddLog("=== Fixing All Prefabs ===");

        // Find player prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Developer/Prefabs" });
        int totalPrefabs = prefabGuids.Length;
        int currentPrefab = 0;

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            currentPrefab++;
            EditorUtility.DisplayProgressBar("Fixing Prefabs", $"Processing {System.IO.Path.GetFileName(prefabPath)}...",
                (float)currentPrefab / totalPrefabs);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                // Check if this is a player prefab
                if (prefab.name.Contains("Player") || prefab.name.Contains("Opponent"))
                {
                    AddLog($"\n--- Fixing Prefab: {prefab.name} ---");
                    FixPlayerPrefab(prefab, prefabPath);
                }
            }
        }

        EditorUtility.ClearProgressBar();
        AddLog("\n=== Prefab Fix Complete ===");
    }

    private void FixPlayerPrefab(GameObject prefab, string prefabPath)
    {
        bool modified = false;

        // Load prefab as editable
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

        // Check for BowController
        BowController bowController = prefabInstance.GetComponent<BowController>();
        if (bowController == null)
        {
            bowController = prefabInstance.GetComponentInChildren<BowController>();
        }

        if (bowController == null)
        {
            AddLog($"✗ No BowController found in {prefab.name}");
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            return;
        }

        // Note: PlayerNetworkLocalSync and PlayerNetworkRemoteSync are added at runtime by GameManager
        // based on player order, so we don't add them to prefabs here
        // This prevents conflicts and ensures correct assignment at runtime

        // Ensure PlayerController or OpponentController exists
        PlayerController playerController = prefabInstance.GetComponent<PlayerController>();
        OpponentController opponentController = prefabInstance.GetComponent<OpponentController>();

        if (playerController == null && opponentController == null)
        {
            // Determine which controller to add based on prefab name
            if (prefab.name.Contains("Player") && !prefab.name.Contains("Opponent"))
            {
                playerController = prefabInstance.AddComponent<PlayerController>();
                AddLog($"✓ Added PlayerController to {prefab.name}");
                modified = true;
            }
            else if (prefab.name.Contains("Opponent"))
            {
                opponentController = prefabInstance.AddComponent<OpponentController>();
                AddLog($"✓ Added OpponentController to {prefab.name}");
                modified = true;
            }
        }

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            AddLog($"✓ Saved prefab: {prefab.name}");
        }
        else
        {
            AddLog($"✓ Prefab already fixed: {prefab.name}");
        }

        PrefabUtility.UnloadPrefabContents(prefabInstance);
    }

    private GameObject FindPrefabByName(params string[] names)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Developer/Prefabs" });
        
        // Try exact matches first
        foreach (string name in names)
        {
            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab != null && prefab.name == name)
                {
                    return prefab;
                }
            }
        }

        // Then try contains matches
        foreach (string name in names)
        {
            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab != null && prefab.name.Contains(name))
                {
                    return prefab;
                }
            }
        }

        return null;
    }

    private void AddLog(string message)
    {
        fixLog.Add(message);
        //Debug.Log($"[MultiplayerSceneFixer] {message}");
    }
}
