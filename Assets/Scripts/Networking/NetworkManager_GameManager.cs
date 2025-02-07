using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections;
using System.Collections.Generic;

public class NetworkManager_GameManager : MonoBehaviour
{
    [SerializeField] public Dictionary<ulong, PlayerClass> m_playerList = new Dictionary<ulong, PlayerClass>();
    [SerializeField] private List<PlayerData> playerList = new List<PlayerData>();
    [SerializeField] private GameObject builderPrefab;
    [SerializeField] private GameObject shooterPrefab;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider loadingSlider;

    [System.Serializable]
    public class PlayerData
    {
        public ulong clientId;
        public PlayerClass playerClass;
    }

    public enum PlayerClass
    {
        Default,
        Builder,
        Shooter
    }

    [Rpc(SendTo.Server)]
    public void AddPlayerRpc(ulong clientId, PlayerClass chosenClass)
    {
        if (!m_playerList.ContainsKey(clientId))
        {
            m_playerList.Add(clientId, chosenClass);

            PlayerData newPlayer = new PlayerData
            {
                clientId = clientId,
                playerClass = chosenClass
            };
            playerList.Add(newPlayer);

            Debug.Log($"Player {clientId} selected class: {chosenClass}");
        }
        else
        {
            Debug.LogWarning($"Player {clientId} is already assigned a class.");
        }
    }

    public List<PlayerData> GetPlayers()
    {
        return playerList;
    }

    [Rpc(SendTo.Server)]
    public void StartGameRpc()
    {
        // Notify clients to load the next scene
        NotifyClientsToLoadSceneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyClientsToLoadSceneRpc()
    {
        string nextSceneName = "SampleScene";

        NetworkManager networkManager = GetComponent<NetworkManager>();
        networkManager.SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);

        StartCoroutine(WaitToSpawnPlayers());
        
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyClientsToUnloadSceneRpc()
    {
        string previousSceneName = "MainMenu";
        Scene previousScene = SceneManager.GetSceneByName(previousSceneName);

        NetworkManager networkManager = GetComponent<NetworkManager>();
        networkManager.SceneManager.UnloadScene(previousScene);
    }    


    // Coroutine to wait for scene load to spawn players
    private IEnumerator WaitToSpawnPlayers()
    {
        yield return new WaitForSeconds(1);
        SpawnPlayersRpc();
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayersRpc()
    {
        foreach (var playerData in playerList)
        {
            GameObject playerPrefab = null;

            // Choose the correct prefab based on player class
            if (playerData.playerClass == PlayerClass.Builder)
            {
                playerPrefab = builderPrefab;
            }
            else if (playerData.playerClass == PlayerClass.Shooter)
            {
                playerPrefab = shooterPrefab;
            }
            else
            {
                Debug.LogWarning($"Player {playerData.clientId} has an unsupported class {playerData.playerClass}. Defaulting to Builder.");
                playerPrefab = builderPrefab;
            }

            if (playerPrefab != null)
            {
                GameObject playerObject = Instantiate(playerPrefab);
                NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();
                networkObject.SpawnAsPlayerObject(playerData.clientId);

                Debug.Log($"Player {playerData.clientId} spawned as {playerData.playerClass}.");

            }
        }
    }
}
