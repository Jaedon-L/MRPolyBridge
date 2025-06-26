using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [Header("DANGER!!!! DO NOT EDIT THESE LISTS!!")]
    [Header("Use the ADD LEVEL or ROMOVE LEVEL button instead")]

    // A list that stores all the Level ScriptableObject instances
    [ReadOnly]
    [SerializeField] public List<Level> levels = new List<Level>();
    [ReadOnly]
    [SerializeField] public List<Button> levelButtons = new List<Button>();

    [SerializeField] private GameManager gameManager;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private GameObject menu;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadLevelStates();
        InitializeButtonState();
    }

    /// <summary>
    /// Adds a new button to the buttons list.
    /// This method creates a new button and assigns it a name based on the number of existing levels.
    /// </summary>
    public void AddButton()
    {
        // Create a new instance of the Level ScriptableObject
        Button newButton = Instantiate(buttonPrefab, buttonParent);
        TMP_Text buttonTxt = newButton.GetComponentInChildren<TMP_Text>();
        buttonTxt.text = "L" + (levelButtons.Count + 1);
        // Assign a name to the level button based on the current count of levels
        newButton.name = "LevelButton " + (levelButtons.Count + 1);

        // Add the newly created button to the buttons list
        levelButtons.Add(newButton);
    }

    /// <summary>
    /// Removes the last button from the buttons list.
    /// This method will only remove a button if there are any buttons in the list.
    /// </summary>
    public void RemoveButton()
    {
        // Check if there are any buttons in the list before attempting to remove one
        if (levelButtons.Count > 0)
        {
            Button levelButton = levelButtons[levelButtons.Count - 1];
            DestroyImmediate(levelButton.gameObject);
            // Remove the last button from the list (using the index of the last item)
            levelButtons.RemoveAt(levelButtons.Count - 1);
        }
    }

    /// <summary>
    /// Load the unlocked state for each level from PlayerPrefs.
    /// </summary>
    private void LoadLevelStates()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            int levelNumer = i + 1;
            string key = "Level_" + levelNumer.ToString(); // Create the key
            if (PlayerPrefs.HasKey(key)) // Check if the key exists
            {
                int isUnlocked = PlayerPrefs.GetInt(key); // Get the saved state (1 for unlocked, 0 for locked)
                levels[i].isUnlocked = isUnlocked == 1;
                Debug.Log(key + " progress has been initialized");
            }
            else
            {
                // If there's no saved data for this level, assume it's locked by default
                levels[i].isUnlocked = false;
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
            int index = i;
            levelButtons[i].onClick.AddListener(() => gameManager.SpawnCurrentLevel(index));
            levelButtons[i].interactable = levels[i].isUnlocked;
            // menu.SetActive(false);
        }

    }
    [ContextMenu("toggleSettings")]
    public void ToggleSettings()
    {

        menu.SetActive(!menu.activeSelf);
    }

}