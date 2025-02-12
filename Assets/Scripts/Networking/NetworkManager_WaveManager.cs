using UnityEngine;
using System.Collections;

public class NetworkManager_WaveManager : MonoBehaviour
{
    [SerializeField] private int m_waveCount = 1; // Int for the wave number
    [SerializeField] private int m_waveTimer = 30; // Int for the timer that counts down between waves
    private const int m_waveTimerMax = 30; // Const Int for the timers max value that counts
    [SerializeField] private int m_enemyCount = 10; // Const Int for the timers max value that counts
    [SerializeField] private GameObject m_enemyPrefab; // Const Int for the timers max value that counts
    [SerializeField] private PlayerController[] playerControllers;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator StartWaveManager()
    {
        yield return StartCoroutine(StartWaveTimer());

        yield return StartWave();
    }

    private IEnumerator StartWaveTimer()
    {
        // Reset the wave timer
        m_waveTimer = m_waveTimerMax;

        while (m_waveTimer > 0)
        {
            yield return new WaitForSeconds(1);
            m_waveTimer--;

            SetWaveTimer(m_waveTimer);
        }
    }

    private IEnumerator StartWave()
    {
        // Increment the wave number2
        m_waveCount++;

        yield return new WaitForSeconds(1);
    }

    private void SetWaveCount(int waveCount)
    {
        // Update the players wave numbers
    }

    private void SetWaveTimer(int waveTimer)
    {
        // Update player huds
    }

}
