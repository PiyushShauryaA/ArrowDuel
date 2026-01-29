# Multiplayer Scene Fixer - Editor Script Guide

## Overview

The **Multiplayer Scene Fixer** is an editor script that automatically fixes all multiplayer-related issues in scenes and prefabs, and assigns all missing references.

## How to Use

### Access the Tool

1. Open Unity Editor
2. Go to **Tools** → **Multiplayer Scene Fixer**
3. A window will open with options

### Available Actions

#### 1. **Fix All Scenes**
- Scans all scenes in `Assets/_Developer/_Scenes/`
- Fixes GameManager references
- Assigns player prefabs automatically
- Fixes ArrowduelConnectionManager UI references
- Creates missing managers (ArrowduelNetworkManager, UnityMainThreadDispatcher)

#### 2. **Fix All Prefabs**
- Scans all prefabs in `Assets/_Developer/Prefabs/`
- Adds missing components to player prefabs
- Validates component setup

#### 3. **Fix Everything (Scenes + Prefabs)**
- Runs both operations in sequence
- Most comprehensive fix option

## What Gets Fixed

### GameManager Fixes

**Player Prefabs:**
- Automatically finds and assigns `player1NetworkStatePrefab`
- Automatically finds and assigns `player2NetworkStatePrefab`
- Validates prefabs have correct controllers

**UI References:**
- Finds and assigns `player1NameText` (TextMeshProUGUI)
- Finds and assigns `player2NameText` (TextMeshProUGUI)
- Finds and assigns `waitingPanel` (GameObject)
- Finds and assigns `windIndicators` (GameObject array)
- Finds and assigns `arrowHitEffect` (GameObject)

### ArrowduelConnectionManager Fixes

**UI References:**
- Finds and assigns `connectButton` (Button)
- Finds and assigns `statusText` (TextMeshProUGUI)
- Finds and assigns `usernameInput` (TMP_InputField)

### Scene Setup

**Creates Missing Managers:**
- `ArrowduelNetworkManager` GameObject (if missing)
- `UnityMainThreadDispatcher` GameObject (if missing)

### Prefab Fixes

**Component Validation:**
- Ensures `PlayerController` or `OpponentController` exists
- Validates `BowController` is present
- Note: Network sync components are added at runtime by GameManager

## Search Logic

The script uses intelligent search to find references:

### Prefab Search
1. **Exact name match** (highest priority)
2. **Contains match** (fallback)
3. Searches in: `Assets/_Developer/Prefabs/`

### UI Element Search
- Searches by name keywords:
  - Player1/P1 → `player1NameText`
  - Player2/P2 → `player2NameText`
  - Connect → `connectButton`
  - Status → `statusText`
  - Username/Name → `usernameInput`
  - Waiting → `waitingPanel`
  - Wind → `windIndicators`

## Fix Log

The tool displays a detailed log showing:
- ✓ Successfully fixed items
- ✗ Errors or missing items
- ⚠ Warnings (found but validation failed)

## Example Output

```
=== Fixing All Scenes ===

--- Fixing Scene: GameScene.unity ---
Found GameManager, fixing references...
✓ Assigned player1NetworkStatePrefab: Player Prefab
✓ Assigned player2NetworkStatePrefab: Opponent Prefab
✓ Assigned player1NameText: Player1Name
✓ Assigned player2NameText: Player2Name
✓ Assigned waitingPanel: WaitingPanel
✓ Assigned 4 wind indicators
✓ Scene saved

Found ArrowduelConnectionManager, fixing references...
✓ Assigned connectButton: ConnectButton
✓ Assigned statusText: StatusText
✓ Assigned usernameInput: UsernameInput

Creating ArrowduelNetworkManager...
Creating UnityMainThreadDispatcher...
✓ Scene saved

=== Scene Fix Complete ===
```

## Best Practices

1. **Run Before Testing**: Always run "Fix Everything" before testing multiplayer
2. **Check Log**: Review the fix log to ensure all references were found
3. **Manual Override**: If auto-assignment fails, manually assign in Inspector
4. **Save Scenes**: The tool automatically saves scenes after fixing

## Troubleshooting

### Prefabs Not Found
- Ensure prefabs are in `Assets/_Developer/Prefabs/`
- Check prefab names match expected patterns:
  - Player prefabs: "Player Prefab", "Player_v3 Prefab", or contains "Player"
  - Opponent prefabs: "Opponent Prefab", "Opponent_v3 Prefab", or contains "Opponent"

### UI References Not Found
- Ensure UI elements have descriptive names
- Add keywords to GameObject names (e.g., "ConnectButton", "StatusText")
- The script searches by name, so clear naming helps

### Components Missing
- Run "Fix All Prefabs" to add missing components
- Check that prefabs have `BowController` component
- Ensure `PlayerController` or `OpponentController` exists

## Notes

- **Network Sync Components**: `PlayerNetworkLocalSync` and `PlayerNetworkRemoteSync` are added at runtime by `GameManager.SpawnPlayerNakama()`, not in prefabs
- **Scene Saving**: Scenes are automatically saved after fixes
- **Prefab Saving**: Prefabs are automatically saved after fixes
- **Non-Destructive**: The tool only adds missing references, doesn't remove existing ones

## Integration with Existing Workflow

This tool complements the existing `LoginSceneSetupEditor`:
- `LoginSceneSetupEditor`: Creates login scene UI
- `MultiplayerSceneFixer`: Fixes game scene and prefab references

Both tools work together to ensure complete multiplayer setup.
