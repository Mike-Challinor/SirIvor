using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject m_enemyPrefab;

    private SpriteRenderer sr;

    // Start is called before the first frame update
    void Start()
    {
        // Get the sprite renderer
        sr = GetComponent<SpriteRenderer>();

        // Disable the editor icon for the spawner
        sr.enabled = false;
    }

    public void SpawnEnemy()
    {
        SpawnEnemyRpc();
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
