using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelManagerUI : MonoBehaviour
{
    [SerializeField] private List<Button> levelButtons;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LevelManager levelManager;


    private void Awake()
    {
        LoadLevelStates();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeButtonState();
    }

    /// <summary>
    /// Load the unlocked state for each level from PlayerPrefs.
    /// </summary>
    private void LoadLevelStates()
    {
        for (int i = 0; i < levelManager.levels.Count; i++)
        {
            int levelNumer = i + 1;
            string key = "Level_" + levelNumer.ToString(); // Create the key
            if (PlayerPrefs.HasKey(key)) // Check if the key exists
            {
                int isUnlocked = PlayerPrefs.GetInt(key); // Get the saved state (1 for unlocked, 0 for locked)
                levelManager.levels[i].isUnlocked = isUnlocked == 1;
                Debug.Log(key + " progress has been initialized");
            }
            else
            {
                // If there's no saved data for this level, assume it's locked by default
                levelManager.levels[i].isUnlocked = false;
            }
        }
    }

    /// <summary>
    /// Initialize the level button state
    /// </summary>
    private void InitializeButtonState()
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            levelButtons[i].onClick.AddListener(() => gameManager.SpawnCurrentLevel(i));

            // set the interactable state to the unlocked state of the corresponding level.
            levelButtons[i].interactable = levelManager.levels[i].isUnlocked;
        }
    }
}
