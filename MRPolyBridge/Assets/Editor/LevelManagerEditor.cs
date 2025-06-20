using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelManager))]
public class LevelManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Get the reference to the LevelManager script
        LevelManager levelManager = (LevelManager)target;

        // Draw the default inspector to show the levels list
        DrawDefaultInspector();

        // Add a button to create a new level
        if (GUILayout.Button("Add Level"))
        {
            AddNewLevel(levelManager);
        }

        // Add a button to remove the last level
        if (GUILayout.Button("Remove Level"))
        {
            RemoveLastLevel(levelManager);
        }
    }

    // Add a new level and save it as an asset
    private void AddNewLevel(LevelManager levelManager)
    {
        Level newLevel = ScriptableObject.CreateInstance<Level>();
        newLevel.levelName = "Level " + (levelManager.levels.Count + 1);

        // Save the new level as an asset
        string path = "Assets/Levels/Level" + (levelManager.levels.Count + 1) + ".asset";
        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();

        // Add the newly created level to the list
        levelManager.levels.Add(newLevel);
    }

    // Remove the last level and delete the asset
    private void RemoveLastLevel(LevelManager levelManager)
    {
        if (levelManager.levels.Count > 0)
        {
            // Get the path of the last level asset
            string path = AssetDatabase.GetAssetPath(levelManager.levels[levelManager.levels.Count - 1]);

            // Remove the level from the list
            levelManager.levels.RemoveAt(levelManager.levels.Count - 1);

            // Delete the level asset
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
        }
    }
}
