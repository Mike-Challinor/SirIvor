using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    [SerializeField] public Dictionary<Vector3Int, TileData> m_tileDataMap = new Dictionary<Vector3Int, TileData>();
    [SerializeField] private List<TileGroup> m_tileGroups = new List<TileGroup>();

    private Tilemap m_tilemap;

    [SerializeField] private TileBase[] m_fences;
    [SerializeField] private TileBase[] m_platforms;
    [SerializeField] private TileBase[] m_buildings;
    [SerializeField] private TileBase[] m_trees;

    private List<TileBase[]> m_tileTypeArrays;
    private List<string> m_tileTypeNames;

    [SerializeField] private float m_fenceHealth = 100f;
    [SerializeField] private float m_buildingHealth = 500f;
    [SerializeField] private float m_platformHealth = 200f;

    public struct TileData
    {
        public float CurrentHealth;
        public float MaxHealth;
        public string Type;

        public TileData(float currentHealth, float maxHealth, string type)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            Type = type;
        }

        public void SetHealth(float health)
        {
            CurrentHealth = health;
        }

        public void SetMaxHealth(float maxHealth)
        {
            MaxHealth = maxHealth;
        }
    }

    private void Start()
    {
        InitializeTileTypeArrays();
        m_tilemap = GameObject.FindWithTag("StructuresTilemap").GetComponent<Tilemap>();
        InitializeTileMap();
    }

    private void InitializeTileTypeArrays()
    {
        m_tileTypeArrays = new List<TileBase[]> { m_fences, m_platforms, m_buildings, m_trees };
        m_tileTypeNames = new List<string> { "Fence", "Platform", "Building", "Tree" };
    }

    public void InitializeTileMap()
    {
        BoundsInt bounds = m_tilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tile = m_tilemap.GetTile(position);
                if (tile != null)
                {
                    string tileType = GetTileTypeFromArrays(tile);

                    if (tileType == "Fence")
                    {
                        AddSingleSprite(position, m_fenceHealth, m_fenceHealth, tileType);
                    }
                    else if (tileType == "Platform")
                    {
                        AddPlatformGroup(position);
                    }
                    else if (tileType == "Building")
                    {
                        AddBuildingGroup(position);
                    }
                    else if (tileType == "Tree")
                    {
                        // Do nothing as this does not have health or need to be tracked
                    }
                }
            }
        }
    }

    public void AddSingleSprite(Vector3Int position, float startingHealth, float maxHealth, string tileType)
    {
        m_tileDataMap[position] = new TileData(startingHealth, maxHealth, tileType);
    }

    private void AddPlatformGroup(Vector3Int position)
    {
        TileGroup platformGroup = CreateTileGroup(m_platformHealth);
        AddTileToGroup(platformGroup, position, "Platform");
        Vector3Int abovePosition = new Vector3Int(position.x, position.y + 1, position.z);
        AddTileToGroup(platformGroup, abovePosition, "Platform");

        Debug.Log($"Platform group created at {position} and {abovePosition}");
    }

    private void AddBuildingGroup(Vector3Int position)
    {
        TileGroup buildingGroup = CreateTileGroup(m_buildingHealth);
        for (int xOffset = 0; xOffset <= 9; xOffset++)
        {
            for (int yOffset = 0; yOffset <= 2; yOffset++)
            {
                Vector3Int buildingPosition = new Vector3Int(position.x + xOffset, position.y + yOffset, position.z);
                AddTileToGroup(buildingGroup, buildingPosition, "Building");
            }
        }

        Debug.Log($"Building group created with tiles from {position} to {(position.x + 9, position.y + 2)}");
    }

    public void AddTileToGroup(TileGroup group, Vector3Int tilePosition, string type)
    {
        if (m_tilemap.HasTile(tilePosition))
        {
            group.AddTile(tilePosition);
            var tileData = new TileData(group.SharedHealth.CurrentHealth, group.SharedHealth.MaxHealth, type);
            m_tileDataMap[tilePosition] = tileData;

            Debug.Log($"Added tile {tilePosition} to group with shared health {group.SharedHealth.CurrentHealth}");
        }
        else
        {
            // No tilemap at location
        }
    }

    public TileGroup CreateTileGroup(float initialHealth)
    {
        var group = new TileGroup(initialHealth);
        m_tileGroups.Add(group);
        Debug.Log($"Created new tile group with shared health: {initialHealth}");
        return group;
    }

    public string GetTileTypeFromArrays(TileBase tile)
    {
        for (int i = 0; i < m_tileTypeArrays.Count; i++)
        {
            foreach (TileBase tileType in m_tileTypeArrays[i])
            {
                if (tile == tileType)
                {
                    return m_tileTypeNames[i];
                }
            }
        }
        return "Unknown";
    }

    public float? GetTileHealth(Vector3Int tilePosition)
    {
        if (m_tileDataMap.TryGetValue(tilePosition, out TileData tileData))
        {
            return tileData.CurrentHealth;
        }

        Debug.LogWarning($"Tile at {tilePosition} does not exist.");
        return null;
    }

    public float? GetMaxHealth(Vector3Int tilePosition)
    {
        if (m_tileDataMap.TryGetValue(tilePosition, out TileData tileData))
        {
            return tileData.MaxHealth;
        }

        Debug.LogWarning($"Tile at {tilePosition} does not exist.");
        return null;
    }

    public void SetTileHealth(Vector3Int tilePosition, float healthToAdd)
    {
        if (m_tileDataMap.TryGetValue(tilePosition, out TileData tileData))
        {
            // Modify the current health
            tileData.CurrentHealth += healthToAdd;

            // Prevent current health from exceeding the max health
            if (tileData.CurrentHealth > tileData.MaxHealth)
            {
                tileData.CurrentHealth = tileData.MaxHealth;
            }

            // Update the tile data in the dictionary
            m_tileDataMap[tilePosition] = tileData;

            // Log the updated health
            Debug.Log($"{healthToAdd} health has been added to the tile data at position: {tilePosition}. Current health = {tileData.CurrentHealth}");
        }
        else
        {
            Debug.LogWarning("Unable to get the tile data value from the tilePosition passed through");
        }
    }

    public bool IsTileInGroup(Vector3Int tilePosition)
    {
        // Check if the tile is part of any group
        foreach (TileGroup group in m_tileGroups)
        {
            if (group.GetTiles().Contains(tilePosition))
            {
                return true;
            }
        }
        return false;
    }

    public TileGroup GetTileGroup(Vector3Int tilePosition)
    {
        // Find and return the group the tile belongs to
        foreach (TileGroup group in m_tileGroups)
        {
            if (group.GetTiles().Contains(tilePosition))
            {
                return group;
            }
        }
        return null; // Return null if no group is found
    }


    public void UpdateTileGroupHealth(TileGroup group, float amount)
    {
        group.UpdateHealth(amount);
        group.SyncHealthAcrossTiles(m_tileDataMap);
        Debug.Log($"Updated group health to {group.SharedHealth.CurrentHealth}");
    }

    public float GetMaxHealthByType(string tileType)
    {
        switch (tileType)
        {
            case "Fence":
                return m_fenceHealth;

            case "Platform":
                return m_platformHealth;

            case "Building":
                return m_buildingHealth;
        }

        return 100f;
    }
}

