using UnityEngine;
using System.Collections.Generic;

public class TileGroup
{
    public struct HealthData
    {
        public float CurrentHealth;
        public float MaxHealth;

        public HealthData(float currentHealth, float maxHealth)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }

    // Store the health data
    public HealthData SharedHealth { get; private set; }
    private List<Vector3Int> m_tiles;

    // Constructor that initializes health using HealthData
    public TileGroup(float initialHealth)
    {
        // Use HealthData to store health information
        SharedHealth = new HealthData(initialHealth, initialHealth);
        m_tiles = new List<Vector3Int>();
    }

    // Add a tile to the group
    public void AddTile(Vector3Int tilePosition)
    {
        if (!m_tiles.Contains(tilePosition))
        {
            m_tiles.Add(tilePosition);
        }
    }

    // Remove a tile from the group
    public void RemoveTile(Vector3Int tilePosition)
    {
        m_tiles.Remove(tilePosition);
    }

    // Update the health for the group (if necessary, e.g., when any tile gets damaged)
    public void UpdateHealth(float amount)
    {
        // Create a new HealthData with updated health
        SharedHealth = new HealthData(
            Mathf.Clamp(SharedHealth.CurrentHealth + amount, 0, SharedHealth.MaxHealth),
            SharedHealth.MaxHealth
        );
    }

    // Notify all tiles in the group to update their health values
    public void SyncHealthAcrossTiles(Dictionary<Vector3Int, TileManager.TileData> tileDataMap)
    {
        foreach (var tilePosition in m_tiles)
        {
            if (tileDataMap.ContainsKey(tilePosition))
            {
                tileDataMap[tilePosition] = new TileManager.TileData(SharedHealth.CurrentHealth, SharedHealth.MaxHealth, tileDataMap[tilePosition].Type);
            }
        }
    }

    // Get the tiles in the group
    public List<Vector3Int> GetTiles()
    {
        return m_tiles;
    }
}
