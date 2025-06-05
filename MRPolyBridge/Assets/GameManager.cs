using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("LEVEL SETUP")]
    [Tooltip("Drag your level‐prefabs here in order: Level 1, Level 2, …")]
    [SerializeField] private List<GameObject> _levelPrefabs;

    [Header("CAR & TARGET")]
    [Tooltip("The only 'car' GameObject or tag you use. We check collisions with the finish zone.")]
    [SerializeField] private string _carTag = "Car"; // assume your car has tag "Player"
    [Tooltip("Exactly one LevelEndTrigger in each level prefab. We will subscribe at runtime.")]
    [SerializeField] private GameObject _levelEndTriggerPrefab;
    // (This is optional if your LevelPrefabs already include the trigger; see later notes.)

    [Header("UI REFERENCES")]
    [Tooltip("Button you press to start level or go to next level.")]
    [SerializeField] private GameObject _startOrNextButton;
    [Tooltip("Text component that shows 'Level: X'.")]
    [SerializeField] private TextMeshPro _levelLabel;
    [Tooltip("Panel (or any GameObject) you show when the player wins.")]
    [SerializeField] private GameObject _youWinPanel;
    [SerializeField] private TextMeshPro winningText;

    private int _currentLevelIndex = 0;        // zero‐based index into _levelPrefabs
    private GameObject _currentLevelInstance;  // the spawned "level" root GameObject

    private enum GameState { WaitingToStart, Playing, LevelComplete, AllFinished }
    private GameState _state = GameState.WaitingToStart;
    // ───── NEW: FOUR “DIRECTIONAL CONTROL” OBJECTS ───────────────────────
    // Each of these must have an InteractableUnityEventWrapper component on it.
    [Header("CAR CONTROLS")]
    [Tooltip("Drag the GameObject that has InteractableUnityEventWrapper for 'Up'")]
    [SerializeField] private GameObject _upControl;
    [Tooltip("Drag the GameObject that has InteractableUnityEventWrapper for 'Down'")]
    [SerializeField] private GameObject _downControl;
    [Tooltip("Drag the GameObject that has InteractableUnityEventWrapper for 'Left'")]
    [SerializeField] private GameObject _leftControl;
    [Tooltip("Drag the GameObject that has InteractableUnityEventWrapper for 'Right'")]
    [SerializeField] private GameObject _rightControl;
    // ────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Hook up our Start/Next button:
        _startOrNextButton.GetComponent<InteractableUnityEventWrapper>().WhenSelect.AddListener(OnStartOrNextPressed);

        // Initially hide the Win panel:
        if (_youWinPanel != null)
            _youWinPanel.SetActive(false);

        // Show “Level 1” but don’t spawn anything yet:
        UpdateLevelLabel();
    }


    /// <summary>
    /// Called when the “Start Game” or “Next Level” button is pressed.
    /// </summary>
    [ContextMenu("Start")]
    private void OnStartOrNextPressed()
    {
        switch (_state)
        {
            case GameState.WaitingToStart:
                // First time: spawn level 0
                SpawnCurrentLevel();
                _state = GameState.Playing;
                _startOrNextButton.SetActive(false);
                break;

            case GameState.LevelComplete:
                // We just won, so move to next level if any remain
                AdvanceToNextLevel();
                break;

            case GameState.AllFinished:
                // (Optional) restart from Level 1 or disable UI
                Debug.Log("All levels completed!");
                winningText.text = "You finished all the levels! Stay tuned for more!!";
                break;

            // While “Playing,” the button should be hidden or disabled
            // so the user can’t press it again.
            case GameState.Playing:
                // do nothing
                break;
        }
    }

    /// <summary>
    /// Spawns the prefab at _currentLevelIndex, and wires up its LevelEndTrigger.
    /// </summary>
    private void SpawnCurrentLevel()
    {
        if (_currentLevelIndex < 0 || _currentLevelIndex >= _levelPrefabs.Count)
        {
            Debug.LogError($"[GameManager] Invalid level index {_currentLevelIndex}");
            return;
        }

        // 1) Destroy any leftover level from before:
        if (_currentLevelInstance != null)
            Destroy(_currentLevelInstance);
            ClearAllBridgePieces();


        // 2) Instantiate the new level at origin
        _currentLevelInstance = Instantiate(
            _levelPrefabs[_currentLevelIndex],
            Vector3.zero,
            Quaternion.identity
        );

        BridgeWalker walker = _currentLevelInstance.GetComponentInChildren<BridgeWalker>();
        if (walker == null)
        {
            Debug.LogError("[GameManager] Could not find a BridgeWalker in the spawned level.");
        }
        else
        {
            // 2) Wire up all four controls to this walker instance:
            WireUpCarControls(walker);
        }

        // 3) Find (or create) the trigger that detects when the car finishes.
        //    We assume each level prefab either:
        //      a) already has a child GameObject with LevelEndTrigger attached, OR
        //      b) you supply a separate "_levelEndTriggerPrefab" you parent under this level.
        //
        LevelEndTrigger trigger = _currentLevelInstance.GetComponentInChildren<LevelEndTrigger>();
        if (trigger == null && _levelEndTriggerPrefab != null)
        {
            // If the level prefab did not include one, instantiate a fresh one:
            GameObject go = Instantiate(_levelEndTriggerPrefab, Vector3.zero, Quaternion.identity);
            go.transform.SetParent(_currentLevelInstance.transform, false);
            trigger = go.GetComponent<LevelEndTrigger>();
            if (trigger == null)
            {
                Debug.LogError("[GameManager] The LevelEndTriggerPrefab has no LevelEndTrigger component.");
            }
        }

        if (trigger != null)
        {
            // Listen for its callback:
            trigger.Initialize(_carTag, OnLevelCompleted);
        }
        else
        {
            Debug.LogWarning($"[GameManager] Level {_currentLevelIndex + 1} has no LevelEndTrigger. It will never end.");
        }

        Debug.Log($"[GameManager] Spawned Level #{_currentLevelIndex + 1}.");
        UpdateLevelLabel();
    }

    /// <summary>
    /// Called by LevelEndTrigger when the car enters the finish zone.
    /// </summary>
    private void OnLevelCompleted()
    {
        if (_state != GameState.Playing) return;

        _state = GameState.LevelComplete;
        Debug.Log($"[GameManager] Level {_currentLevelIndex + 1} Complete!");

        // Show "You Win!" panel:
        if (_youWinPanel != null)
            _youWinPanel.SetActive(true);

        // Re‐enable the Start/Next button so the player can advance:
        _startOrNextButton.SetActive(true);
        _startOrNextButton.GetComponentInChildren<TextMeshPro>().text = "Next Level";

        // If this was the last level, change button text accordingly:
        if (_currentLevelIndex == _levelPrefabs.Count - 1)
        {
            _startOrNextButton.GetComponentInChildren<TextMeshPro>().text = "Finish";
            _state = GameState.AllFinished;
        }
    }

    /// <summary>
    /// Called after the player presses “Next Level” once they’ve seen the win panel.
    /// </summary>
    private void AdvanceToNextLevel()
    {
        // Hide the Win panel
        if (_youWinPanel != null)
            _youWinPanel.SetActive(false);

        // Move to the next index
        _currentLevelIndex++;
        if (_currentLevelIndex < _levelPrefabs.Count)
        {
            // Spawn and immediately go to PLAYING
            SpawnCurrentLevel();
            _state = GameState.Playing;

            // Disable the button while playing
            _startOrNextButton.SetActive(false);
            _startOrNextButton.GetComponentInChildren<TextMeshPro>().text = "Playing...";
        }
        else
        {
            // We have actually finished all levels
            Debug.Log("[GameManager] You have beaten every level!");
            _startOrNextButton.SetActive(false);
            _startOrNextButton.GetComponentInChildren<TextMeshPro>().text = "All Done!";
        }
    }

    /// <summary>
    /// Updates the UI text that says “Level: X”.
    /// </summary>
    private void UpdateLevelLabel()
    {
        if (_levelLabel != null)
        {
            _levelLabel.text = $"Level: {_currentLevelIndex + 1}";
        }
    }
    /// <summary>
    /// Given a BridgeWalker on the newly spawned car, hook up each of the four
    /// InteractableUnityEventWrapper controls to call MoveUp/MoveDown/MoveLeft/MoveRight.
    /// </summary>
    private void WireUpCarControls(BridgeWalker walker)
    {
        // For each control we must:
        //   a) Check if the assigned GameObject is not null.
        //   b) Get its InteractableUnityEventWrapper component.
        //   c) Register the appropriate method on BridgeWalker.

        if (_upControl != null)
        {
            var wrapper = _upControl.GetComponent<InteractableUnityEventWrapper>();
            if (wrapper != null)
            {
                // Clear any previous listeners, then add MoveUp
                wrapper.WhenSelect.RemoveAllListeners();
                wrapper.WhenSelect.AddListener(walker.StartMoveUp);
                wrapper.WhenUnselect.AddListener(walker.StopMove);
            }
            else
            {
                Debug.LogWarning("[GameManager] _upControl has no InteractableUnityEventWrapper.");
            }
        }

        if (_downControl != null)
        {
            var wrapper = _downControl.GetComponent<InteractableUnityEventWrapper>();
            if (wrapper != null)
            {
                wrapper.WhenSelect.RemoveAllListeners();
                wrapper.WhenSelect.AddListener(walker.StartMoveDown);
                wrapper.WhenUnselect.AddListener(walker.StopMove);
            }
            else
            {
                Debug.LogWarning("[GameManager] _downControl has no InteractableUnityEventWrapper.");
            }
        }

        if (_leftControl != null)
        {
            var wrapper = _leftControl.GetComponent<InteractableUnityEventWrapper>();
            if (wrapper != null)
            {
                wrapper.WhenSelect.RemoveAllListeners();
                wrapper.WhenSelect.AddListener(walker.StartMoveLeft);
                wrapper.WhenUnselect.AddListener(walker.StopMove);
            }
            else
            {
                Debug.LogWarning("[GameManager] _leftControl has no InteractableUnityEventWrapper.");
            }
        }

        if (_rightControl != null)
        {
            var wrapper = _rightControl.GetComponent<InteractableUnityEventWrapper>();
            if (wrapper != null)
            {
                wrapper.WhenSelect.RemoveAllListeners();
                wrapper.WhenSelect.AddListener(walker.StartMoveRight);
                wrapper.WhenUnselect.AddListener(walker.StopMove);
            }
            else
            {
                Debug.LogWarning("[GameManager] _rightControl has no InteractableUnityEventWrapper.");
            }
        }

        Debug.Log($"[GameManager] Wired up controls to BridgeWalker on {walker.gameObject.name}");
    }
    /// <summary>
    /// Call this (for example, from a UI “Clear” button) to destroy every
    /// SnapInteractable node, hinge‐beam, and support‐beam in the scene.
    /// </summary>
    public void ClearAllBridgePieces()
    {
        // 1) Destroy every SnapInteractable (nodes):
        var allNodes = FindObjectsByType<SnapInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < allNodes.Length; i++)
        {
            Destroy(allNodes[i].transform.root.gameObject);
        }

        // 2) Destroy every beam (anything with a HingeJoint):
        var allHinges = FindObjectsByType<HingeJoint>(FindObjectsSortMode.None);
        for (int i = 0; i < allHinges.Length; i++)
        {
            Destroy(allHinges[i].gameObject);
        }

        // 3) Destroy any support beams (anything with a SupportTracker):
        var allSupports = FindObjectsByType<SupportTracker>(FindObjectsSortMode.None);
        for (int i = 0; i < allSupports.Length; i++)
        {
            Destroy(allSupports[i].gameObject);
        }

        // Optionally, clear any internal graph data right away:
        // (If you want to be sure BridgeGraph has no leftover references.)
        BridgeGraph.ClearAll();   // ← see note below

        Debug.Log("[GameManager] Cleared all nodes, beams, and support‐beams.");
    }
}
