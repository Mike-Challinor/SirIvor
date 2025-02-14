using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    [SerializeField] public Dictionary<Vector3Int, TileData> m_tileDataMap = new Dictionary<Vector3Int, TileData>();
    [SerializeField] private List<TileGroup> m_tileGroups = new List<TileGroup>();

    [SerializeField] private List<Vector3Int> m_fencePositions = new List<Vector3Int>();
    [SerializeField] private List<Vector3Int> m_buildingPositions;

    [SerializeField] private Tilemap m_tilemap;

    [SerializeField] private TileBase[] m_fences;
    [SerializeField] private TileBase[] m_platforms;
    [SerializeField] private TileBase[] m_buildings;
    [SerializeField] private TileBase[] m_trees;

    private List<TileBase[]> m_tileTypeArrays;
    private List<string> m_tileTypeNames;

    [SerializeField] private float m_fenceHealth = 100f;
    [SerializeField] private float m_buildingHealth = 500f;
    [SerializeField] private float m_platformHealth = 200f;

    [SerializeField] private GameObject m_navMesh;
    public NavMeshPlus.Components.NavMeshSurface m_navMeshSurface;

    [SerializeField] private GameObject[] m_players;

    private NetworkManager_GameManager m_gameManager;

    // Tiledata struct that includes health, type, a constructor and methods for setting health
    public struct TileData
    {
        public float CurrentHealth;
        public float MaxHealth;
        public string Type;

        // Constructor that sets health and type
        public TileData(float currentHealth, float maxHealth, string type)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            Type = type;
        }

        // Method for setting health on a tile
        public void SetHealth(float health)
        {
            CurrentHealth = health;
        }

        // Method for setting max health on a tile
        public void SetMaxHealth(float maxHealth)
        {
            MaxHealth = maxHealth;
        }
    }

    private void Start()
    {
        // Init the tile type arrays
        InitializeTileTypeArrays();

        // Get reference to the tilemap
        m_tilemap = GameObject.FindWithTag("StructuresTilemap").GetComponent<Tilemap>();

        // Init the tilemap
        InitializeTileMap();

        // Get refs to nav mesh surface and the gamemanager
        m_navMeshSurface = m_navMesh.GetComponent<NavMeshPlus.Components.NavMeshSurface>();
        m_gameManager = FindAnyObjectByType<NetworkManager_GameManager>();
    }

    // Method for initialising the tile type arrays
    private void InitializeTileTypeArrays()
    {
        m_tileTypeArrays = new List<TileBase[]> { m_fences, m_platforms, m_buildings, m_trees };
        m_tileTypeNames = new List<string> { "Fence", "Platform", "Building", "Tree" };
    }

    // Method for initialising the tilemap
    public void InitializeTileMap()
    {
        // Sets reference for tilemaps bounds
        BoundsInt bounds = m_tilemap.cellBounds;

        // Loop through the bounds
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                // Get the tile at the current position
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tile = m_tilemap.GetTile(position);

                // Check for if there is a tile in this position
                if (tile != null)
                {
                    // Get the tiles type
                    string tileType = GetTileTypeFromArrays(tile);

                    // If statement that executes logic depending on the tiles type
                    if (tileType == "Fence")
                    {
                        // Add as a single sprite with the appropriate health and type
                        AddSingleSprite(position, m_fenceHealth, m_fenceHealth, tileType);

                        // Add to fence positions list
                        m_fencePositions.Add(position);
                    }
                    else if (tileType == "Platform")
                    {
                        // Create a platform group if one has not been created
                        if (!IsTileInGroup(position))
                        {
                            AddPlatformGroup(position);
                        }
                        
                    }
                    else if (tileType == "Building")
                    {
                        // Create a building group if one has not been created
                        if (!IsTileInGroup(position))
                        {
                            AddBuildingGroup(position);
                        }

                        // Add to building positions list
                        m_buildingPositions.Add(position);

                    }
                    else if (tileType == "Tree")
                    {
                        // Do nothing as this does not have health or need to be tracked
                    }
                }
            }
        }
    }

    // Method for finding and setting the players into an array
    private void SetPlayers()
    {
        m_players = GameObject.FindGameObjectsWithTag("Player");
    }

    // Method for adding a single sprite to tiledata map
    public void AddSingleSprite(Vector3Int position, float startingHealth, float maxHealth, string tileType)
    {
        m_tileDataMap[position] = new TileData(startingHealth, maxHealth, tileType);
    }

    // Method for adding a platform group
    private void AddPlatformGroup(Vector3Int position)
    {
        // Create the tile group with platforms health
        TileGroup platformGroup = CreateTileGroup(m_platformHealth);

        // Add the first tile to the group
        AddTileToGroup(platformGroup, position, "Platform");

        // Get the position of the next tile
        Vector3Int abovePosition = new Vector3Int(position.x, position.y + 1, position.z);

        // Add the second tile to the group
        AddTileToGroup(platformGroup, abovePosition, "Platform");

    }

    // Method for adding a building group
    private void AddBuildingGroup(Vector3Int position)
    {
        // Create the tile group with the buildings health
        TileGroup buildingGroup = CreateTileGroup(m_buildingHealth);

        // Loop through each offset position for the building (10x4)
        for (int xOffset = 0; xOffset <= 9; xOffset++)
        {
            for (int yOffset = 0; yOffset <= 3; yOffset++)
            {
                // Set the current position based off of the loops offset values
                Vector3Int buildingPosition = new Vector3Int(position.x + xOffset, position.y + yOffset, position.z);

                // Add the tile to the group
                AddTileToGroup(buildingGroup, buildingPosition, "Building");
            }
        }
    }

    // Method for adding a tile to a group
    public void AddTileToGroup(TileGroup group, Vector3Int tilePosition, string type)
    {
        // Check to
        if (m_tilemap.HasTile(tilePosition))
        {
            // Add the tile to the group
            group.AddTile(tilePosition);

            // Create tiledata for the tile
            var tileData = new TileData(group.SharedHealth.CurrentHealth, group.SharedHealth.MaxHealth, type);

            // Add the tiledata to the data map
            m_tileDataMap[tilePosition] = tileData;

        }
    }

    // Method for creating a tile group
    public TileGroup CreateTileGroup(float initialHealth)
    {
        // Create the tilegroup
        var group = new TileGroup(initialHealth);

        // Add group to the list of tile groups
        m_tileGroups.Add(group);

        // Return the group
        return group;
    }

    // Accessor method that returns the tiles type
    public string GetTileTypeFromArrays(TileBase tile)
    {
        // Loop through the tile types array
        for (int i = 0; i < m_tileTypeArrays.Count; i++)
        {
            foreach (TileBase tileType in m_tileTypeArrays[i])
            {
                // If the tile matches the tiletype
                if (tile == tileType)
                {
                    // Return this tiletype
                    return m_tileTypeNames[i];
                }
            }
        }
        // Return tile type is unknown
        return "Unknown";
    }

    // Accessor method that returns the current health of a single tile
    public float? GetTileHealth(Vector3Int tilePosition)
    {
        // If there is data in the data map from the position passed through
        if (m_tileDataMap.TryGetValue(tilePosition, out TileData tileData))
        {
            // Return the current health of that tile
            return tileData.CurrentHealth;
        }

        Debug.LogWarning($"Tile at {tilePosition} does not exist.");
        return null;
    }

    // Accessor method that returns the max health of a single tile
    public float? GetMaxHealth(Vector3Int tilePosition)
    {
        // If there is data in the data map from the position passed through
        if (m_tileDataMap.TryGetValue(tilePosition, out TileData tileData))
        {
            // Return the max health of that tile
            return tileData.MaxHealth;
        }

        Debug.LogWarning($"Tile at {tilePosition} does not exist.");
        return null;
    }

    // Method for adding health to a single tile
    public void AddTileHealth(Vector3Int tilePosition, float healthToAdd)
    {
        // If there is data in the data map from the position passed through
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

        }
        else
        {
            Debug.LogWarning("Unable to get the tile data value from the tilePosition passed through");
        }
    }

    // Method for removing health from a single tile
    public void RemoveTileHealth(Vector3Int tilePosition, float healthToRemove)
    {
        // If there is data in the data map from the position passed through
        if (m_tileDataMap.TryGetValue(tilePosition, out TileData tileData))
        {
            // Modify the current health
            tileData.CurrentHealth -= healthToRemove;

            // Prevent current health from falling below 0
            if (tileData.CurrentHealth <= 0)
            {
                tileData.CurrentHealth = 0;
                RemoveTileServerRpc(tilePosition);
            }

            // Update the tile data in the dictionary
            m_tileDataMap[tilePosition] = tileData;

            // Log the updated health
            Debug.Log($"{healthToRemove} health has been removed from the tile data at position: {tilePosition}. Current health = {tileData.CurrentHealth}");
        }
        else
        {
            Debug.LogWarning("Unable to get the tile data value from the tilePosition passed through");
        }
    }

    // Server rpc for removing a single tile
    [Rpc(SendTo.Everyone)]
    private void RemoveTileServerRpc(Vector3Int position)
    {
        // Set tile to null
        m_tilemap.SetTile(position, null);

        // Rebake nav mesh
        m_navMeshSurface.BuildNavMesh();

    }

    // Accessor method that returns whether a tileposition belongs to a group
    public bool IsTileInGroup(Vector3Int tilePosition)
    {
        // Loop through each of the tilegroups
        foreach (TileGroup group in m_tileGroups)
        {
            // Check if the tile is part of any group
            if (group.GetTiles().Contains(tilePosition))
            {
                return true;
            }
        }
        return false;
    }

    // Accessor method that returns what group a tile belongs to
    public TileGroup GetTileGroup(Vector3Int tilePosition)
    {
        // Loop through each of the tilegroups
        foreach (TileGroup group in m_tileGroups)
        {
            // Find and return the group the tile belongs to
            if (group.GetTiles().Contains(tilePosition))
            {
                return group;
            }
        }
        return null; // Return null if no group is found
    }

    // Accessor method that returns all the fence positions
    public List<Vector3Int> GetFencePositions()
    {
        return m_fencePositions;
    }

    // Accessor method that returns all of the building positions
    public List<Vector3Int> GetBuildingPositions()
    {
        return m_buildingPositions;
    }

    // Accessor method that returns the structures tilemap
    public Tilemap GetTilemap()
    {
        return m_tilemap;
    }

    // Method for updating the health of a tilegroup
    public void UpdateTileGroupHealth(TileGroup group, float amount)
    {
        // If players have not been set, then populate the players array
        if (m_players != null) { SetPlayers(); }

        // Update the groups health
        group.UpdateHealth(amount);

        // Sync the health across all tiles in the group
        group.SyncHealthAcrossTiles(m_tileDataMap);

        // If it is a building group
        if (group.SharedHealth.MaxHealth == 500)
        {
            // Update the health bar for each playerHUD
            foreach (GameObject player in m_players)
            {
                player.GetComponent<PlayerHUD>().updateHealth(group.SharedHealth.CurrentHealth);
            }
        }

        // Check for game over
        if (group.SharedHealth.CurrentHealth <= 0 && group.SharedHealth.MaxHealth == 500)
        {
            m_gameManager.SetCurrentGameState(NetworkManager_GameManager.GameState.GameEnded);
        }

        Debug.Log($"Updated group health to {group.SharedHealth.CurrentHealth}");
    }

    // Accessor method that returns the max health by the tiles type
    public float GetMaxHealthByType(string tileType)
    {
        // Switch that returns the corresponding health based off of the tiletype passed through
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

