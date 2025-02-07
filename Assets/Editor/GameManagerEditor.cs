using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(NetworkManager_GameManager))]
public class GameManagerEditor : Editor
{
    private NetworkManager_GameManager gameManager;

    private void OnEnable()
    {
        gameManager = (NetworkManager_GameManager)target;
    }

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        List<NetworkManager_GameManager.PlayerData> players = gameManager.GetPlayers();

        // Check if the player list is populated
        if (players == null || players.Count == 0)
        {
            EditorGUILayout.HelpBox("No players added yet.", MessageType.Info);
        }
        else
        {
            // Display the player list content
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Players", EditorStyles.boldLabel);

            foreach (var player in players)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Client ID: {player.clientId}");
                EditorGUILayout.LabelField($"Player Class: {player.playerClass}");
                EditorGUILayout.EndVertical();
            }
        }
    }
}
