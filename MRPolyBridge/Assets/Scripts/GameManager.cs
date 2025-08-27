using Oculus.Interaction;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("LEVEL SETUP")]
    [Tooltip("Drag your level scriptableObject here in order: Level 1, Level 2, …")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private HandPinchDetection pinchDetection;
    [SerializeField] private SpawnPointSelector spawnPointSelector;

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
    [SerializeField] private TextMeshProUGUI winningText;
    [Header("WIN SCREEN STARS")]
    [Tooltip("Assign the 3 star GameObjects (or Images) in order: star1, star2, star3")]
    [SerializeField] private GameObject[] _starIcons = new GameObject[3];
    private float budgetToUse;
    [SerializeField] private SteeringWheelUse steeringWheel;

    private Vector3 spawnPosition;
    private int _currentLevelIndex = 0;        // zero‐based index into _levelPrefabs
    private GameObject _currentLevelInstance;  // the spawned "level" root GameObject

    private enum GameState { WaitingToStart, Playing, LevelComplete, AllFinished }
    private GameState _state = GameState.WaitingToStart;

    private void Awake()
    {
        // Hook up our Start/Next button:
        _startOrNextButton.GetComponent<InteractableUnityEventWrapper>().WhenSelect.AddListener(OnStartOrNextPressed);

        // Initially hide the Win panel:
        if (_youWinPanel != null)
            _youWinPanel.SetActive(false);

        // Initialize the players current level
        LoadCurrentLevel();

        // Show “Level 1” but don’t spawn anything yet:
        UpdateLevelLabel();

        // Saving the current level locked state for level 1
        if (_currentLevelIndex == 0)
            SaveLevelState(_currentLevelIndex, true);
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
                SpawnCurrentLevel(_currentLevelIndex);
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
    /// Load the player's current level from the saved data
    /// </summary>
    private void LoadCurrentLevel()
    {
        if (PlayerPrefs.HasKey("CurrentLevel"))
        {
            _currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel");
        }
        else
        {
            // Default to the first level if no data is found
            _currentLevelIndex = 0;
        }
    }

    /// <summary>
    /// Spawns the prefab at _currentLevelIndex, and wires up its LevelEndTrigger.
    /// </summary>
    public void SpawnCurrentLevel(int levelNumber)
    {
        spawnPosition = spawnPointSelector.GetLockedSpawnPoint();
        //if (_currentLevelIndex < 0 || _currentLevelIndex >= _levelPrefabs.Count)
        if (levelNumber < 0 || levelNumber >= levelManager.levels.Count)
        {
            Debug.LogError($"[GameManager] Invalid level index {levelNumber}");
            return;
        }

        // 1) Destroy any leftover level from before:
        if (_currentLevelInstance != null)
            Destroy(_currentLevelInstance);
        ClearAllBridgePieces();


        // 2) Instantiate the new level at origin
        _currentLevelInstance = Instantiate(
           levelManager.levels[levelNumber].levelPrefab,
            spawnPosition,
            Quaternion.identity
        );
        _currentLevelIndex = levelNumber;
        InitializeLevelBridgeSettings(levelNumber);


        // Reset budget based on level
        budgetToUse = GetBudgetForLevel(levelNumber);
        BudgetManager.Instance.ResetBudget(budgetToUse);


        // 3) Find (or create) the trigger that detects when the car finishes.
        //    We assume each level prefab either:
        //      a) already has a child GameObject with LevelEndTrigger attached, OR
        //      b) you supply a separate "_levelEndTriggerPrefab" you parent under this level.
        
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
            Debug.LogWarning($"[GameManager] Level {levelNumber + 1} has no LevelEndTrigger. It will never end.");
        }
        if (steeringWheel != null)
        {
            var car = FindFirstObjectByType<PrometeoCarController>();
            steeringWheel.SetCarController(car); 
        }
        Debug.Log($"[GameManager] Spawned Level #{levelNumber + 1}.");
        UpdateLevelLabel();
    }

    /// <summary>
    /// Initializes the break force and torque threshold for each level.
    /// </summary>
    private void InitializeLevelBridgeSettings(int levelIndex)
    {
        var levelData = levelManager.levels[levelIndex];

        pinchDetection.breakForceThreshold = levelData.breakForceThreshold;
        pinchDetection.breakTorqueThreshold = levelData.breakTorqueThreshold;

        pinchDetection.supportBonusForce = levelData.supportBonusForce;
        pinchDetection.supportBonusTorque = levelData.supportBonusTorque;

        BridgePhysicsConfig config = new()
        {
            baseBreakForce = levelData.breakForceThreshold,
            baseBreakTorque = levelData.breakTorqueThreshold,
            supportBonusForce = levelData.supportBonusForce,
            supportBonusTorque = levelData.supportBonusTorque
        };

        BridgeGraph.SetConfig(config);
    }
    //Budget settings
    private float GetBudgetForLevel(int idx)
    {
        return levelManager.levels[idx].budget;
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

        // Update stars
        UpdateStarsOnWinScreen();

        int starsEarned = CalculateStarsEarned();
        string starKey = $"Level_{_currentLevelIndex + 1}_Stars";
        int previous = PlayerPrefs.GetInt(starKey, 0);
        PlayerPrefs.SetInt(starKey, Mathf.Max(previous, starsEarned));
        PlayerPrefs.Save();

        UpdateEndLevelInfoText();
        // Re‐enable the Start/Next button so the player can advance:
        _startOrNextButton.SetActive(true);
        _startOrNextButton.GetComponentInChildren<TextMeshPro>().text = "Next Level";

        // If this was the last level, change button text accordingly:
        if (_currentLevelIndex == levelManager.levels.Count - 1)
        {
            _startOrNextButton.GetComponentInChildren<TextMeshPro>().text = "Finish";
            _state = GameState.AllFinished;
        }

        SaveLevelData();
    }

    /// <summary>
    /// Saves the current level's data, including advancing to the next level and updating its unlocked state.
    /// </summary>
    private void SaveLevelData()
    {
        // Increment the current level index to move to the next level
        _currentLevelIndex++;

        if (_currentLevelIndex < levelManager.levels.Count)
        {
            // Save the current level index to PlayerPrefs so the player can resume from the same level
            SaveCurrentLevel();

            // Unlock the level at the new index (set the level's isUnlocked state to true)
            levelManager.levels[_currentLevelIndex].isUnlocked = true;

            // Save the unlocked state of the level to PlayerPrefs so it persists across sessions
            SaveLevelState(_currentLevelIndex, true);
            // *** NEW: Immediately re‐load & re‐initialize your LevelManager UI ***
            levelManager.LoadLevelStates();
            levelManager.InitializeButtonState();
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

        if (_currentLevelIndex < levelManager.levels.Count)
        {
            // Spawn and immediately go to PLAYING
            SpawnCurrentLevel(_currentLevelIndex);
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
    /// Save the player current level
    /// </summary>
    private void SaveCurrentLevel()
    {
        PlayerPrefs.SetInt("CurrentLevel", _currentLevelIndex);
        PlayerPrefs.Save();
        Debug.Log(_currentLevelIndex + " has been saved");
    }

    /// <summary>
    /// Save the unlocked state of the current level to PlayerPrefs.
    /// </summary>
    private void SaveLevelState(int level, bool isUnlocked)
    {
        int levelIndex = level + 1;
        string key = "Level_" + levelIndex.ToString(); // This could be something like "Level_1", "Level_2", etc.
        PlayerPrefs.SetInt(key, isUnlocked ? 1 : 0); // Save the unlocked state (1 = unlocked, 0 = locked)
        PlayerPrefs.Save();
        Debug.Log(key + " has been unlocked");
    }
    /// <summary>
    /// Reads the remaining budget and the current level's star thresholds,
    /// then turns on the appropriate number of stars.
    /// </summary>
    private void UpdateStarsOnWinScreen()
    {
        // // 1) Get remaining budget
        // float remaining = BudgetManager.Instance.GetCurrentBudget();

        // // 2) Read thresholds
        // var thresholds = levelManager.levels[_currentLevelIndex].starThresholds;

        // 3) Determine how many stars
        int starsEarned = CalculateStarsEarned();
        // // thresholds assumed sorted ascending [oneStar, twoStar, threeStar]
        // for (int i = 0; i < thresholds.Length; i++)
        // {
        //     if (remaining >= thresholds[i])
        //         starsEarned = i + 1;
        // }

        // 4) Turn on/off icons
        for (int i = 0; i < _starIcons.Length; i++)
        {
            _starIcons[i].SetActive(i < starsEarned);
        }
    }
    /// <summary>
    /// Calculates how many stars the player earned this level,
    /// based on the remaining budget and the Level.starThresholds array.
    /// </summary>
    /// <returns>Number of stars earned (0–3).</returns>
    public int CalculateStarsEarned()
    {
        // 1. Get the remaining budget from your BudgetManager
        float remaining = BudgetManager.Instance.GetCurrentBudget();

        // 2. Grab the thresholds for the current level
        float[] thresholds = levelManager.levels[_currentLevelIndex].starThresholds;

        // 3. Determine stars earned by seeing how many thresholds were met
        int stars = 0;
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (remaining >= thresholds[i])
                stars = i + 1;  // i=0 → 1 star, i=1 → 2 stars, etc.
        }

        return stars;
    }
    /// <summary>
    /// Updates the end‑of‑level info text to show:
    /// - Budget remaining
    /// - If under 3 stars: how much more to hit the next star thresholds
    /// </summary>
    public void UpdateEndLevelInfoText()
    {
        float remaining = BudgetManager.Instance.GetCurrentBudget();
        var thresholds = levelManager.levels[_currentLevelIndex].starThresholds; // [1★, 2★, 3★]
        int stars = CalculateStarsEarned();

        var sb = new System.Text.StringBuilder();



        if (stars < thresholds.Length)
        {
            // Only show the unmet star levels above what you've already earned
            for (int next = stars; next < thresholds.Length; next++)
            {
                float needed = Mathf.Max(0, thresholds[next] - remaining);
                int starLevel = next + 1;

                // Choose font size based on star level
                float fontSize = starLevel == 1 ? 0.033f :
                                 starLevel == 2 ? 0.038f : 0.04f;

                // Build the inline star icons (next+1 stars)
                string starIcons = "";
                for (int i = 0; i <= next; i++)
                    starIcons += "<sprite name=\"goldStar_0\">";
                // 1) Show remaining budget
                sb.AppendLine($"<size={fontSize}>You saved {remaining:F0} Bridge Bucks.");
                sb.AppendLine($"<size={fontSize}>+{needed:F0} to reach {starIcons}");
            }
        }
        else
        {
            sb.AppendLine($"<size={0.04}>You saved {remaining:F0} Bridge Bucks.");
            // Perfect run (all stars earned)
            sb.AppendLine("<size={0.04}>Perfect run!! You earned all 3 <sprite name=\"goldStar_0\">!");
        }

        if (winningText != null)
            winningText.text = sb.ToString();
    }


    /// <summary>
    /// Updates the UI text that says “Level: X”.
    /// </summary>
    private void UpdateLevelLabel()
    {
        if (_levelLabel != null)
        {
            _levelLabel.text = $"Current Level: {_currentLevelIndex + 1}";
        }
    }
    [ContextMenu("Clear All Saves")]
    private void ClearAllSaves()
    {
        PlayerPrefs.DeleteKey("CurrentLevel");
        // Also delete your per‐level keys:
        for (int i = 1; i <= levelManager.levels.Count; i++)
            PlayerPrefs.DeleteKey($"Level_{i}");
        PlayerPrefs.Save();
        Debug.Log("All PlayerPrefs cleared.");
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
                                  // Reset budget based on level
                                  // float budgetToUse = GetBudgetForLevel(levelNumber);
        BudgetManager.Instance.ResetBudget(budgetToUse);

        Debug.Log("[GameManager] Cleared all nodes, beams, and support‐beams.");
    }
}
