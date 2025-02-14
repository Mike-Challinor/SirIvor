using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class NetworkManager_GameManager : MonoBehaviour
{
    [SerializeField] public Dictionary<ulong, PlayerClass> m_playerList = new Dictionary<ulong, PlayerClass>();
    [SerializeField] private List<PlayerData> playerList = new List<PlayerData>();
    [SerializeField] private List<GameObject> m_playerObjects = new List<GameObject>();
    [SerializeField] private GameObject builderPrefab;
    [SerializeField] private GameObject shooterPrefab;
    [SerializeField] private GameObject m_waveManagerObject;
    [SerializeField] private GameObject m_quitGameMenu;
    [SerializeField] private NetworkManager_WaveManager m_waveManager;
    [SerializeField] private bool m_gameEnded = false;
    // Class with players data
    [System.Serializable]
    public class PlayerData
    {
        public ulong clientId;
        public PlayerClass playerClass;
    }

    // Enum for player classes
    public enum PlayerClass
    {
        Default,
        Builder,
        Shooter
    }

    // Enum for gamestate
    public enum GameState
    {
        Default,
        GameStarted,
        GameEnded
    }

    // Set a reference for the current games state
    private GameState m_currentGameState = GameState.Default;

    private void Update()
    {
        // Check to see if the game has ended
        if (!m_gameEnded && GetCurrentGameState() == GameState.GameEnded)
        {
            // Set the boolean tracker to the game being ended
            m_gameEnded = !m_gameEnded;

            // Call function to end the game
            EndGameRpc();
        }
    }


    [Rpc(SendTo.Server)]
    public void AddPlayerRpc(ulong clientId, PlayerClass chosenClass)
    {
        // Add the player if not already been added
        if (!m_playerList.ContainsKey(clientId)) // Check that clientID has not already been added to list
        {
            // Add to player dictionary
            m_playerList.Add(clientId, chosenClass); 

            // Create and set new player data
            PlayerData newPlayer = new PlayerData 
            {
                clientId = clientId,
                playerClass = chosenClass
            };

            // Add to player list with new playerData
            playerList.Add(newPlayer); 

            Debug.Log($"Player {clientId} selected class: {chosenClass}");
        }
        else
        {
            Debug.LogWarning($"Player {clientId} is already assigned a class.");
        }
    }

    // Method for returning list of player data
    public List<PlayerData> GetPlayers()
    {
        return playerList;
    }

    // Server rpc for starting the game
    [Rpc(SendTo.Server)]
    public void StartGameRpc()
    {
        // Notify everyone to load the next scene
        NotifyClientsToLoadGameSceneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyClientsToLoadGameSceneRpc()
    {
        // Set the scene string to name
        string nextSceneName = "SampleScene";

        // Get reference to the network manager script
        NetworkManager networkManager = GetComponent<NetworkManager>();

        // Load the next scene
        networkManager.SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);

        // Start coroutine to eventually spawn players
        StartCoroutine(WaitToSpawnPlayers());
        
    }

    // Coroutine to wait for scene load to spawn players
    private IEnumerator WaitToSpawnPlayers()
    {
        // Time to wait until game has started
        yield return new WaitForSeconds(5f);

        // Set the current game state to game started
        SetCurrentGameState(GameState.GameStarted);
        
        // Spawn all players
        SpawnPlayersRpc();

        // Start the wave manager
        m_waveManagerObject = GameObject.FindWithTag("WaveManager");
        m_waveManager = m_waveManagerObject.GetComponent<NetworkManager_WaveManager>();
        m_waveManager.SetGameManager(this);
        m_waveManager.StartWaveManager();

    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayersRpc()
    {
        // Loop through data for each player in the playerList
        foreach (var playerData in playerList)
        {
            // Reset the player prefab
            GameObject playerPrefab = null;

            // Set reference to player spawn objects
            GameObject[] playerSpawns = GameObject.FindGameObjectsWithTag("PlayerSpawn");

            // Create a reference for the players transform
            Transform playerTransform = null;

            // Choose the correct prefab based on player class
            if (playerData.playerClass == PlayerClass.Builder) // If player chose builder class
            {
                // Set to relevant prefab for the builder class
                playerPrefab = builderPrefab;
                playerTransform = playerSpawns[1].transform;
            }

            else if (playerData.playerClass == PlayerClass.Shooter) // If player chose shooter class
            {
                // Set to relevant prefab for shooter class
                playerPrefab = shooterPrefab;
                playerTransform = playerSpawns[0].transform;
            }

            else // Debug checking for unsupported class in case game started without choice
            {
                Debug.LogWarning($"Player {playerData.clientId} has an unsupported class {playerData.playerClass}. Defaulting to Builder.");

                // Default to builder class
                playerPrefab = builderPrefab;
            }

            // Spawn the player
            if (playerPrefab != null) // Check that player prefab has been set
            {
                // Instantiate the object locally
                GameObject playerObject = Instantiate(playerPrefab, playerTransform);

                // Add the gameobject to a list (to refer back to when ending the game)
                m_playerObjects.Add(playerObject);
                //playerObject.transform.SetParent(playerTransform);

                // Spawn the player object
                NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();
                networkObject.SpawnAsPlayerObject(playerData.clientId);

                Debug.Log($"Player {playerData.clientId} spawned as {playerData.playerClass}.");

            }
        }
    }

    public void SetCurrentGameState(GameState newState)
    {
        m_currentGameState = newState;
        Debug.Log($"Game State changed to: {m_currentGameState}");
    }

    public GameState GetCurrentGameState()
    {
        return m_currentGameState;
    }

    [Rpc(SendTo.Server)]
    private void EndGameRpc()
    {
        NotifyClientsToLoadEndGameSceneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyClientsToLoadEndGameSceneRpc()
    {
        foreach (GameObject player in m_playerObjects)
        {
            Player_Input_Handler playerInput = player.GetComponent<Player_Input_Handler>();

            if (playerInput != null)
            {
                playerInput.SetCanMove(false);
            }
        }

        // Set the scene string to name
        string nextSceneName = "EndGame";

        // Get reference to the network manager script
        NetworkManager networkManager = GetComponent<NetworkManager>();

        // Load the next scene
        networkManager.SceneManager.LoadScene(nextSceneName, LoadSceneMode.Additive);

    }
}
