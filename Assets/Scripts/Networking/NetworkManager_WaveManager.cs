using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;

public class NetworkManager_WaveManager : NetworkBehaviour
{
    [SerializeField] private int m_waveCount = 0; // Int for the wave number
    [SerializeField] private int m_waveTimer = 10; // Int for the timer that counts down between waves
    [SerializeField] private int m_spawnTimer = 2; // Int for the timer that counts down between waves
    private const int m_waveTimerMax = 15; // Const Int for the timers max value that counts
    [SerializeField] private int m_initialEnemyCount = 12; // Int for the number of initial enemy count
    [SerializeField] private GameObject[] m_enemySpawners; // Game object array for enemy spawners
    [SerializeField] private int m_enemyCount; // Int to count number of enemies
    [SerializeField] private GameObject[] m_players; // List of players
    [SerializeField] private List<PlayerController> m_playerControllers = new List<PlayerController>(); // Player controller script list
    [SerializeField] private NetworkManager_GameManager m_gameManager; // Game manager script
    [SerializeField] private bool m_waveStarted = false; // Boolean to track whether wave has started

    NetworkVariable<int> m_waveCountNetwork = new NetworkVariable<int>(0);
    NetworkVariable<int> m_waveTimerNetwork = new NetworkVariable<int>(10);


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Subscribe to network variables on value changed
        m_waveCountNetwork.OnValueChanged += Handle_WaveCount_OnValueChangedRpc;
        m_waveTimerNetwork.OnValueChanged += Handle_WaveTimer_OnValueChangedRpc;
    }

    // Timer function for starting the wave manager coroutine
    public void StartWaveManager()
    {
        // Get references to the player
        GetPlayerRefsRpc();

        // Start the wave timer coroutine
        StartCoroutine(StartWaveManagerTimer());
    }

    // Timer function for starting the wave manager timer
    private IEnumerator StartWaveManagerTimer()
    {
        // Get reference to the spawners and store in member variable
        GameObject[] spawners = GameObject.FindGameObjectsWithTag("EnemySpawner");
        m_enemySpawners = spawners;

        // Continue running wave manager until game ends
        while (m_gameManager.GetCurrentGameState() == NetworkManager_GameManager.GameState.GameStarted)
        {
            // Start timer
            yield return StartCoroutine(StartWaveTimer());

            // Start next wave
            yield return StartCoroutine(StartWave());

            // Wait for end of wave
            yield return StartCoroutine(WaitForWaveEnd());
        }

        // Game ended
    }

    // Timer function for starting the wave timer
    private IEnumerator StartWaveTimer()
    {
        // Reset the wave timer
        m_waveTimer = m_waveTimerMax;
        m_waveTimerNetwork.Value = m_waveTimer;

        // While loop do decrease timer until it reaches 0
        while (m_waveTimer > 0)
        {
            // Wait a second for updating timer
            yield return new WaitForSeconds(1);

            // Set wave timer and update playerHUDs
            SetWaveTimer();
        }
    }

    // Timer function for starting the next wave
    private IEnumerator StartWave()
    {
        // Set the wave started variable to prevent starting another wave
        m_waveStarted = true;

        // Set the wave count and update the playerHUD
        SetWaveCount();

        // Set the amount of total enemies to spawn
        int enemiesToSpawn = GetNumberOfEnemiesToSpawn();

        // Set the enemy count
        SetEnemyCount(enemiesToSpawn);

        // Delay before spawning enemies
        yield return new WaitForSeconds(1);

        // For loop for the total enemies that spawns each enemy at random spawner location
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Timer to wait before spawning each enemy
            yield return new WaitForSeconds(m_spawnTimer);

            // Spawn enemy on the chosen spawner
            m_enemySpawners[GenerateRandomSpawnerIndex(1, m_enemySpawners.Length)].GetComponent<EnemySpawner>().SpawnEnemy();
        }
    }

    // Timer function for waiting for the wave to end
    private IEnumerator WaitForWaveEnd()
    {
        // Loop and return null until the wave has ended
        while (m_waveStarted)
        {
            yield return null;
        }

        // Slight delay before ending the wave
        yield return new WaitForSeconds(1);
    }

    // Function for setting the wave count
    private void SetWaveCount()
    {
        // Increment the wave count
        m_waveCount++;
        Debug.Log($"Wave count is: {m_waveCount}");

        // Update the playersHUDs with current wave count
        m_waveCountNetwork.Value = m_waveCount;
    }

    private void SetWaveTimer()
    {
        // Decrement the wave timer
        m_waveTimer--;

        // Update the playersHUDs with current wave timer
        m_waveTimerNetwork.Value = m_waveTimer;
    }

    private int GenerateRandomSpawnerIndex(int min, int max)
    {
        // Return a random number between passed through variables
        return Random.Range(min, max);
    }

    // Method for returning the number of enemies for the current wave
    private int GetNumberOfEnemiesToSpawn()
    {
        // Use a quadratic formula to increase the enemy amount based off of the wave count
        return m_initialEnemyCount + (m_waveCount * m_waveCount);
    }

    // Method for ending the wave
    private void EndWave()
    {
        // Set the checking boolean variable to false
        m_waveStarted = false;

        Debug.Log($"Wave {m_waveCount} has ended.");
    }

    // Method for setting the enemy count
    private void SetEnemyCount(int amount)
    {
        m_enemyCount = amount;
    }

    // Method for decrementing the enemy count
    public void RemoveEnemyCount()
    {
        m_enemyCount--;

        if (m_enemyCount <= 0)
        {
            EndWave();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void Handle_WaveTimer_OnValueChangedRpc(int previousValue, int newValue)
    {
        foreach (PlayerController player in m_playerControllers)
        {
            player.SetWaveTimer(newValue);

            // If the timer is zero fade the text out
            if (newValue == 0)
            {
                player.FadeWaveTimer(false);
            }

            // If the timer has been reset to the max value then fade text in
            else if (newValue == m_waveTimerMax)
            {
                player.FadeWaveTimer(true);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void Handle_WaveCount_OnValueChangedRpc(int previousValue, int newValue)
    {
        foreach (PlayerController player in m_playerControllers)
        {
            player.SetWaveCount(newValue);

            // If it is the first wave then fade in the text
            if (newValue == 1)
            {
                player.FadeInWaveCount();
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void GetPlayerRefsRpc()
    {
        // Get reference to all players in the game
        m_players = GameObject.FindGameObjectsWithTag("Player");

        // Loop through each player and grab references to their player controller script
        foreach (var player in m_players)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                m_playerControllers.Add(controller);
            }
        }
    }

    public void SetGameManager(NetworkManager_GameManager gameManager)
    {
        m_gameManager = gameManager;
    }

}
