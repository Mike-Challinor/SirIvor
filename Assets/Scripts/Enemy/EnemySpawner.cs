using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private float m_spawnTimer = 5.5f;
    [SerializeField] private GameObject m_enemyPrefab;
    [SerializeField] private bool m_isActive = false;

    private SpriteRenderer sr;

    // Start is called before the first frame update
    void Start()
    {
        // Get the sprite renderer
        sr = GetComponent<SpriteRenderer>();

        // Disable the editor icon for the spawner
        sr.enabled = false;

        // Start the spawn timer
        StartCoroutine(EnemySpawnTimer());
    }

    IEnumerator EnemySpawnTimer()
    {
        while (m_isActive)
        {
            // Wait time
            yield return new WaitForSeconds(m_spawnTimer);

            // Only invoke the server RPC on the server
            if (IsServer)
            {
                SpawnEnemyRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    void SpawnEnemyRpc()
    {
        // Instantiate the enemy prefab on the server
        GameObject enemyToSpawn = Instantiate(m_enemyPrefab, transform.position, transform.rotation);

        // Spawn the enemy on all clients
        enemyToSpawn.GetComponent<NetworkObject>().Spawn();
    }
}
