using UnityEngine;
using System.Collections;

public class NetworkManager_WaveManager : MonoBehaviour
{
    [SerializeField] private int m_waveCount = 1; // Int for the wave number
    [SerializeField] private int m_waveTimer = 30; // Int for the timer that counts down between waves
    private const int m_waveTimerMax = 30; // Const Int for the timers max value that counts
    [SerializeField] private int m_initialEnemyCount = 20; // Int for the number of initial enemy count
    [SerializeField] private GameObject[] m_enemySpawners; // Const Int for the timers max value that counts
    [SerializeField] private PlayerController[] m_playerControllers;
    [SerializeField] private NetworkManager_GameManager m_gameManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_gameManager = GetComponent<NetworkManager_GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator StartWaveManager()
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
            yield return StartWave();
        }
        
        // Game ended
    }

    private IEnumerator StartWaveTimer()
    {
        // Reset the wave timer
        m_waveTimer = m_waveTimerMax;

        // While loop do decrease timer until it reaches 0
        while (m_waveTimer > 0)
        {
            // Wait a second for updating timer
            yield return new WaitForSeconds(1);

            // Set wave timer and update playerHUDs
            SetWaveTimer(m_waveTimer);
        }
    }

    private IEnumerator StartWave()
    {
        // Set the wave count and update the playerHUD
        SetWaveCount();

        // Set the amount of total enemies to spawn
        int enemiesToSpawn = GetNumberOfEnemiesToSpawn();

        // Delay before spawning enemies
        yield return new WaitForSeconds(1);

        // For loop for the total enemies that spawns each enemy at random spawner location
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Spawn enemy on the chosen spawner
            m_enemySpawners[GenerateRandomSpawnerIndex(1, m_enemySpawners.Length)].GetComponent<EnemySpawner>().SpawnEnemy();
        }
        
        

    }

    private void SetWaveCount()
    {
        // Increment the wave count
        m_waveCount++;

        // Update the playersHUDs with current wave count
    }

    private void SetWaveTimer(int waveTimer)
    {
        // Decrement the wave timer
        m_waveTimer--;

        // Update the playersHUDs with current wave timer
    }

    private int GenerateRandomSpawnerIndex(int min, int max)
    {
        // Return a random number between passed through variables
        return Random.Range(min, max);
    }

    private int GetNumberOfEnemiesToSpawn()
    {
        // Use a quadratic formula to increase the enemy amount based off of the wave count
        return m_initialEnemyCount + (m_waveCount * m_waveCount);
    }

}
