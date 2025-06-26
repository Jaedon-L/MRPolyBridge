using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof (LevelManagerUI))]
public class LevelManager : MonoBehaviour
{
    // A list that stores all the Level ScriptableObject instances
    public List<Level> levels = new List<Level>();
    public LevelManagerUI levelManagerUI;

    /// <summary>
    /// Create a button for the new level
    /// </summary>
    public void AddLevelButton()
    {
        levelManagerUI.AddButton();
    }

    /// <summary>
    /// Deletes the corresponidng button of the level
    /// </summary>
    public void RemoveLevelButton()
    {
        levelManagerUI.RemoveButton();
    }
}