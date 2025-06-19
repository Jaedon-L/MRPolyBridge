using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    // A list that stores all the Level ScriptableObject instances
    public List<Level> levels = new List<Level>();

    /// <summary>
    /// Adds a new level to the levels list.
    /// This method creates a new Level ScriptableObject and assigns it a name based on the number of existing levels.
    /// </summary>
    public void AddLevel()
    {
        // Create a new instance of the Level ScriptableObject
        Level newLevel = ScriptableObject.CreateInstance<Level>();

        // Assign a name to the level based on the current count of levels
        newLevel.levelName = "Level " + (levels.Count + 1);

        // Add the newly created level to the levels list
        levels.Add(newLevel);
    }

    /// <summary>
    /// Removes the last level from the levels list.
    /// This method will only remove a level if there are any levels in the list.
    /// </summary>
    public void RemoveLevel()
    {
        // Check if there are any levels in the list before attempting to remove one
        if (levels.Count > 0)
        {
            // Remove the last level from the list (using the index of the last item)
            levels.RemoveAt(levels.Count - 1);
        }
    }
}