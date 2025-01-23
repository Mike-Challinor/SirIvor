using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(TileManager))]
public class TileManagerEditor : Editor
{
    private TileManager tileManager;

    private void OnEnable()
    {
        tileManager = (TileManager)target;
    }

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Check if the dictionary is populated
        if (tileManager.m_tileDataMap.Count == 0)
        {
            EditorGUILayout.HelpBox("No tiles added yet.", MessageType.Info);
        }
        else
        {
            // Display the dictionary content
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tile Data Map", EditorStyles.boldLabel);

            foreach (var entry in tileManager.m_tileDataMap)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Position: {entry.Key}");
                EditorGUILayout.LabelField($"Type: {entry.Value.Type}");
                EditorGUILayout.LabelField($"Health: {entry.Value.CurrentHealth.ToString()}");
                EditorGUILayout.EndVertical();
            }
        }

        // Optionally, add buttons for actions like InitializeTileMap
        if (GUILayout.Button("Initialize Tile Map"))
        {
            tileManager.InitializeTileMap();
        }
    }
}
