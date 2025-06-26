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
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Cannot Modify Levels in Play Mode",
                "Adding or removing levels is disabled during Play Mode.\nPlease exit Play Mode first.",
                "OK"
            );
            return;
        }

        Level newLevel = ScriptableObject.CreateInstance<Level>();
        newLevel.levelName = "Level " + (levelManager.levels.Count + 1);

        // Save the new level as an asset
        string path = "Assets/Levels/Level" + (levelManager.levels.Count + 1) + ".asset";
        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();

        // Add the newly created level to the list
        levelManager.levels.Add(newLevel);

        // Add the newly created button for the level to the list
        levelManager.AddButton();

        //Ensure changes are saved to the LevelManager
        EditorUtility.SetDirty(levelManager);
    }

    // Remove the last level and delete the asset
    private void RemoveLastLevel(LevelManager levelManager)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Cannot Modify Levels in Play Mode",
                "Adding or removing levels is disabled during Play Mode.\nPlease exit Play Mode first.",
                "OK"
            );
            return;
        }

        if (levelManager.levels.Count > 0)
        {
            // Get the path of the last level asset
            string path = AssetDatabase.GetAssetPath(levelManager.levels[levelManager.levels.Count - 1]);

            // Remove the level from the list
            levelManager.levels.RemoveAt(levelManager.levels.Count - 1);

            // Delete the level asset
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();

            // Delete the corresponding level button
            levelManager.RemoveButton();

            //Ensure changes are saved to the LevelManager
            EditorUtility.SetDirty(levelManager);
        }
    }
}
