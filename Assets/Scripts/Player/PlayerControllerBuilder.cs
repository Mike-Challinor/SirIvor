using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.VisualScripting;

public class PlayerControllerBuilder : PlayerController
{
    private float m_buildTimer = 0.5f;
    private GameObject m_buildModeGameObject;
    private TileBase m_equippedTile;
    private Tilemap m_buildModeTilemap;
    private Tilemap m_structuresTilemap;
    private Vector3Int m_buildTileLocation;
    private float m_buildDistance = 2f;
    private TileManager m_tileManager;
    private int m_currentSelectedStructure = 0;
    private TileBase[][] m_objectTileArray;

    [SerializeField] private bool m_isBuilding = false;
    [SerializeField] private bool m_startBuilding = false;
    [SerializeField] private bool m_canBuild = false;
    [SerializeField] private bool m_isBuildMode = false;
    [SerializeField] public TileBase[] m_platformTileArray;
    [SerializeField] public TileBase[] m_fenceTileArray;
    [SerializeField] private Tilemap[] m_TilemapArray;
    [SerializeField] private GameObject m_buildSprite;
    [SerializeField] private GameObject m_playerHUDLocal;

    private float lastUpdateTime = 0f;
    private const float updateInterval = 0.05f; // 50ms interval for updates
    private Dictionary<Vector3Int, TileBase> localTileCache = new Dictionary<Vector3Int, TileBase>();

    private enum SelectedStructure
    {
        Fence,
        Platform
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Ensure m_objectTileArray is initialized
        m_objectTileArray = new TileBase[2][];  // 2 for Fence and Platform

        if (m_platformTileArray.Length > 0)
        {
            m_objectTileArray[0] = m_platformTileArray; // Add the platform tiles to the object array
            m_equippedTile = m_objectTileArray[m_currentSelectedStructure][0];  // Set the equipped tile
        }

        if (m_fenceTileArray.Length > 0)
        {
            m_objectTileArray[1] = m_fenceTileArray; // Add the fence tiles to the object array
        }

        if (m_equippedTile != null) { SetSprite(m_equippedTile); }

        m_buildModeGameObject = GameObject.FindWithTag("BuildModeTilemap");

        if (m_buildModeGameObject != null)
        {
            m_buildModeTilemap = m_buildModeGameObject.GetComponent<Tilemap>();
            m_TilemapArray[0] = m_buildModeTilemap;
        }

        m_structuresTilemap = GameObject.FindWithTag("StructuresTilemap").GetComponent<Tilemap>();
        m_TilemapArray[1] = m_structuresTilemap;
        m_tileManager = GameObject.FindWithTag("Tilemanager").GetComponent<TileManager>();
    }


    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (!IsOwner) return;

        if (m_isBuilding && !m_startBuilding) // Update logic for when building
        {
            MovePlayerToBuildLocation(m_buildTileLocation, base.GetMoveSpeed());
        }

        if (m_isBuildMode) // Update logic for when in build mode
        {
            if (m_playerCamera == null) // Don't execute code if player camera is null
            {
                Debug.Log("PLAYERCONTROLLERBUILDER::UPDATE:: Player camera is null");
                return;
            }

            // Get mouse position
            Vector3 mousePos = m_playerCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // Check if the mouse is within the screen and other conditions
            if (IsMouseWithinScreen() && Time.timeScale == 1 && Application.isFocused)
            {
                DrawBuildPreviewSprite(mousePos);
            }
        }
    }

    private void DrawBuildPreviewSprite(Vector3 mousePos)
    {
        Vector3Int cellPosition = m_buildModeTilemap.WorldToCell(mousePos);

        // Throttle updates to improve performance
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            lastUpdateTime = Time.time;

            // Remove the previous tiles immediately
            foreach (var cachedTile in localTileCache)
            {
                m_buildModeTilemap.SetTile(cachedTile.Key, null);
            }
            localTileCache.Clear(); // Clear cache for all previously placed tiles

            // Update the build location
            m_buildTileLocation = cellPosition;

            // Set new tiles if equipped
            if (m_equippedTile != null && m_objectTileArray[m_currentSelectedStructure] != null)
            {
                TileBase[] tilesToPlace = m_objectTileArray[m_currentSelectedStructure];  // Get tile array

                if (tilesToPlace.Length >= 4)  // Ensure there are at least 4 tiles
                {
                    // Define L-shaped placement pattern
                    Vector3Int[] offsets = new Vector3Int[]
                    {
                    new Vector3Int(0, 0, 0),  // Base tile (Bottom Left)
                    new Vector3Int(1, 0, 0),  // Right of base
                    new Vector3Int(0, 1, 0),  // Above base
                    new Vector3Int(1, 1, 0)   // Above right tile
                    };

                    for (int i = 0; i < offsets.Length; i++)
                    {
                        Vector3Int tilePosition = cellPosition + offsets[i];
                        TileBase tile = tilesToPlace[i];

                        m_buildModeTilemap.SetTile(tilePosition, tile);
                        localTileCache[tilePosition] = tile;

                        // Sync each tile with the server
                        SetTileOnServerRpc(tilePosition, GetTileIndex(tile), OwnerClientId, GetTilemapIndex(m_buildModeTilemap), true, true);
                    }
                }

                else
                {
                    TileBase tile = tilesToPlace[0];
                    m_buildModeTilemap.SetTile(cellPosition, tile);
                    localTileCache[cellPosition] = tile;
                    SetTileOnServerRpc(cellPosition, GetTileIndex(tile), OwnerClientId, GetTilemapIndex(m_buildModeTilemap), true, true);
                }
            }
            else
            {
                Debug.LogError("Equipped tile is null or structure array is empty");
            }

            // Check if the tile can be built at the location
            if (m_structuresTilemap != null)
            {
                bool canBuildAtAllTiles = true; // Assume we can build until proven otherwise

                // Get the tile positions based on the offsets (for L-shaped, for example)
                List<Vector3Int> tilePositions = new List<Vector3Int>();

                // Add the base tile and the offsets to the list of positions
                TileBase[] tilesToPlace = m_objectTileArray[m_currentSelectedStructure]; // Get tile array
                if (tilesToPlace.Length >= 4)  // L-shaped placement
                {
                    Vector3Int[] offsets = new Vector3Int[]
                    {
                        new Vector3Int(0, 0, 0), // Base tile (Bottom Left)
                        new Vector3Int(1, 0, 0), // Right of base
                        new Vector3Int(0, 1, 0), // Above base
                        new Vector3Int(1, 1, 0)  // Above right tile
                    };

                    foreach (var offset in offsets)
                    {
                        tilePositions.Add(m_buildTileLocation + offset);
                    }
                }
                else // For a single tile
                {
                    tilePositions.Add(m_buildTileLocation);
                }

                // Iterate through each tile position and check if it can be built
                foreach (var tilePosition in tilePositions)
                {
                    TileBase structureTile = m_structuresTilemap.GetTile(tilePosition);

                    // If any tile is not buildable (i.e., it is occupied), we cannot build the structure
                    if (structureTile != null)
                    {
                        m_buildModeTilemap.color = Color.red;
                        canBuildAtAllTiles = false; // Mark that building is not allowed
                        break; // Break out of loop
                    }
                }

                // Set the build mode color and state
                if (canBuildAtAllTiles)
                {
                    m_buildModeTilemap.color = Color.white;
                    m_canBuild = true; // All tiles are free to build
                }
                else
                {
                    m_canBuild = false; // At least one tile is blocked
                }
            }
            else
            {
                m_canBuild = false;
                Debug.LogError("PLAYERCONTROLLERBUILDER::UPDATE:: Structures tilemap is null");
            }
        }
        else
        {
            // Force synchronization if a desync is detected
            SyncTilesWithServer();
        }
    }


    private void MovePlayerToBuildLocation(Vector3Int tileLocation, float speed)
    {
        Debug.Log("PLAYERCONTROLLERBUILDER:: MOVEPLAYERTOBUILDLOCATION:: Function called...");

        float cellWidth = 1f;
        float cellHeight = 1f;

        // Convert Vector3Int to Vector3 (ignoring z if necessary)
        Vector3 position = new Vector3(tileLocation.x + cellWidth / 2, tileLocation.y + cellHeight / 2, transform.position.z);

        // Calculate the distance between player and the sprite to move towards
        float distance = Vector3.Distance(transform.position, position);

        if (distance > m_buildDistance)
        {
            Debug.Log($"Distance equals: {distance} and m_buildDistance equals: {m_buildDistance} ");

            // Calculate direction to the tile
            Vector3 direction = (position - transform.position).normalized;

            m_RB.linearVelocity = direction * speed;
        }

        else
        {
            Debug.Log("PLAYERCONTROLLERBUILDER:: MOVEPLAYERTOBUILDLOCATION:: Reached destination. Building block");
            m_RB.linearVelocity = Vector2.zero;
            StartCoroutine(BuildingTimer());
            m_startBuilding = true;
        }

    }

    public void ToggleBuildMode()
    {
        m_isBuildMode = !m_isBuildMode;

        if (m_isBuildMode)
        {
            EnableBuildMode();
        }
        else
        {
            DisableBuildMode();
        }
    }

    public void EnableBuildMode()
    {
        Debug.Log("PLAYERCONTROLLERBUILDER::ENABLEBUILDMODE:: Function called");
    }

    public void DisableBuildMode()
    {
        Debug.Log("PLAYERCONTROLLERBUILDER::DISABLEBUILDMODE:: Function called");

        // Iterate through the localTileCache and remove each tile from the build mode tilemap
        foreach (var tilePosition in localTileCache.Keys)
        {
            m_buildModeTilemap.SetTile(tilePosition, null); // Remove the tile at the position
        }

        localTileCache.Clear(); // Clear the tile cache

        SetTileOnServerRpc(m_buildTileLocation, -1, OwnerClientId, GetTilemapIndex(m_buildModeTilemap), true, false);
        m_canBuild = false;
    }

    public void StartBuild()
    {
        Debug.Log("PLAYERCONTROLLERBUILDER::STARTBUILD:: Starting build");

        if (!m_isBuilding)
        {
            ToggleBuildMode(); // Disable build mode to start building
            m_isBuilding = true;
            m_RB.linearVelocity = Vector2.zero;
            m_playerInputHandler.SetCanMove(false);

            TileBase[] tilesToPlace = m_objectTileArray[m_currentSelectedStructure];  // Get tile array

            if (tilesToPlace.Length >= 4)  // Ensure there are at least 4 tiles
            {
                SetMultipleSprites(m_objectTileArray[m_currentSelectedStructure], false, true);
            }

            else
            {
                TileBase tile = tilesToPlace[0];
                m_buildModeTilemap.SetTile(m_buildTileLocation, tile);
                localTileCache[m_buildTileLocation] = tile;

                // Sync each tile with the server
                SetTileOnServerRpc(m_buildTileLocation, GetTileIndex(tile), OwnerClientId, GetTilemapIndex(m_structuresTilemap), false, true);
            }

        }

        else
        {
            Debug.Log("PLAYERCONTROLLERBUILDER::STARTBUILD:: Player is already building");
        }

    }

    private IEnumerator BuildingTimer()
    {
        Debug.Log("PLAYERCONTROLLER::BUILDINGTIMER:: Function called...");

        if (m_objectTileArray[m_currentSelectedStructure].Length >= 4)  // Ensure there are at least 4 tiles
        {
            // Create a new tile group
            TileGroup tileGroup = m_tileManager.CreateTileGroup(200);

            // Define L-shaped placement pattern
            Vector3Int[] offsets = new Vector3Int[]
            {
                    new Vector3Int(0, 0, 0),  // Base tile (Bottom Left)
                    new Vector3Int(1, 0, 0),  // Right of base
                    new Vector3Int(0, 1, 0),  // Above base
                    new Vector3Int(1, 1, 0)   // Above right tile
            };

            // Add each tile to the group
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3Int tilePosition = m_buildTileLocation + offsets[i];

                // Add tile to the group
                m_tileManager.AddTileToGroup(tileGroup, tilePosition, "Platform");
            }

            // Update tile groups health and set it to 0
            m_tileManager.UpdateTileGroupHealth(tileGroup, -200);
        }

        else
        {
            // Add the tile to the tile manager
            m_tileManager.AddSingleSprite(m_buildTileLocation, 0, GetMaxHealthOfTileToBuild(), m_tileManager.GetTileTypeFromArrays(m_equippedTile)); // Set starting health to 0
        }

        // Wait for building progress
        yield return StartCoroutine(BuildingProgressTimer()); // Wait for timer for adding health / building

        CancelBuilding();
    }

    private IEnumerator BuildingProgressTimer()
    {
        float healthIncrement = 10f;
        float buildInterval = m_buildTimer;

        // Ensure tile data exists before starting
        if (!m_tileManager.m_tileDataMap.TryGetValue(m_buildTileLocation, out var tileData))
        {
            Debug.LogWarning($"Tile at {m_buildTileLocation} not found!");
            yield break; // Stop the coroutine if no tile data exists
        }

        // Set the player HUD to active
        m_playerHUDLocal.SetActive(true);

        // Continue updating health until it reaches max health
        while (tileData.CurrentHealth < tileData.MaxHealth)
        {
            float remainingHealth = tileData.MaxHealth - tileData.CurrentHealth;

            // Add either the increment or the remaining health, whichever is smaller
            float healthToAdd = Mathf.Min(healthIncrement, remainingHealth);
            m_tileManager.SetTileHealth(m_buildTileLocation, healthToAdd);

            // Get tile's health values
            float? currentHealth = m_tileManager.GetTileHealth(m_buildTileLocation);
            float? maxHealth = m_tileManager.GetMaxHealth(m_buildTileLocation);

            float percentage = (currentHealth.Value / maxHealth.Value); // Percentage between 0 and 1 for the slider

            if (currentHealth.HasValue)
            {
                m_playerHUDLocal.GetComponentInChildren<Slider>().value = percentage;
            }

            Debug.Log($"Added {healthToAdd} health to tile at {m_buildTileLocation}. Current health: {tileData.CurrentHealth}");

            // Check if the tile is part of a group and update health across the group
            if (m_tileManager.IsTileInGroup(m_buildTileLocation))
            {
                TileGroup tileGroup = m_tileManager.GetTileGroup(m_buildTileLocation);
                m_tileManager.UpdateTileGroupHealth(tileGroup, healthToAdd); // Update health of the whole group
            }

            // Wait for the interval before the next update
            yield return new WaitForSeconds(buildInterval);

            // Update tile data reference (to reflect changes made in SetTileHealth)
            tileData = m_tileManager.m_tileDataMap[m_buildTileLocation];
        }

        Debug.Log($"Building at {m_buildTileLocation} has reached max health: {tileData.MaxHealth}");

        // Hide the slider
        m_playerHUDLocal.SetActive(false);

        // Reset the slider value to 0
        m_playerHUDLocal.GetComponentInChildren<Slider>().value = 0;

        TileBase[] tilesToPlace = m_objectTileArray[m_currentSelectedStructure];  // Get tile array

        if (tilesToPlace.Length >= 4)  // Ensure there are at least 4 tiles
        {
            SetMultipleSprites(m_objectTileArray[m_currentSelectedStructure], false, false);
        }
        else
        {
            TileBase tile = tilesToPlace[0];
            m_buildModeTilemap.SetTile(m_buildTileLocation, tile);

            // Sync each tile with the server
            SetTileOnServerRpc(m_buildTileLocation, GetTileIndex(tile), OwnerClientId, GetTilemapIndex(m_structuresTilemap), false, false);
        }
    }


    public void CancelBuilding()
    {
        m_isBuilding = false;
        m_startBuilding = false;
        m_playerInputHandler.SetCanMove(true);
    }
    private float GetMaxHealthOfTileToBuild()
    {
        return m_tileManager.GetMaxHealthByType(m_tileManager.GetTileTypeFromArrays(m_equippedTile));
    }

    private void SetSprite(TileBase tile)
    {
        m_equippedTile = tile;
    }

    private void SetMultipleSprites(TileBase[] tileArray, bool justOwner, bool isOpaque)
    {
        TileBase[] tilesToPlace = tileArray;  // Get tile array

        // Define L-shaped placement pattern
        Vector3Int[] offsets = new Vector3Int[]
        {
                    new Vector3Int(0, 0, 0),  // Base tile (Bottom Left)
                    new Vector3Int(1, 0, 0),  // Right of base
                    new Vector3Int(0, 1, 0),  // Above base
                    new Vector3Int(1, 1, 0)   // Above right tile
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3Int tilePosition = m_buildTileLocation + offsets[i];
            TileBase tile = tilesToPlace[i];

            m_structuresTilemap.SetTile(tilePosition, tile);
            localTileCache[tilePosition] = tile;

            // Sync each tile with the server
            SetTileOnServerRpc(tilePosition, GetTileIndex(tile), OwnerClientId, GetTilemapIndex(m_structuresTilemap), justOwner, isOpaque);
        }
    }

    public void FlipBuildSprite(bool isFacingRight)
    {
        Vector3 localOffset = m_buildSprite.transform.localPosition;

        if (isFacingRight)
        {
            m_buildSprite.transform.localPosition = new Vector3(Mathf.Abs(localOffset.x), localOffset.y, localOffset.z);
        }
        else
        {
            m_buildSprite.transform.localPosition = new Vector3(-Mathf.Abs(localOffset.x), localOffset.y, localOffset.z);
        }
    }

    private void SyncTilesWithServer()
    {
        // Request the server to resend the current state of the tilemap
        RequestTileSyncServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestTileSyncServerRpc()
    {
        // Iterate through all tiles in the current state
        foreach (var position in localTileCache.Keys)
        {
            TileBase tile = localTileCache[position];
            int tileIndex = GetTileIndex(tile);
            UpdateTileOnClientsRpc(position, tileIndex, OwnerClientId, GetTilemapIndex(m_buildModeTilemap), true, true);
        }
    }

    [Rpc(SendTo.Server)]
    void SetTileOnServerRpc(Vector3Int position, int tileIndex, ulong networkObjectId, int tilemapindex, bool justOwner, bool isopaque)
    {
        if (justOwner) // When the tile should show for just the owner
        {
            if (OwnerClientId == networkObjectId && IsOwner)
            {
                // Make sure we are getting the correct tile from the selected structure
                TileBase tile = tileIndex >= 0 && tileIndex < m_objectTileArray[m_currentSelectedStructure].Length
                    ? m_objectTileArray[m_currentSelectedStructure][tileIndex]
                    : null;
                m_TilemapArray[tilemapindex].SetTile(position, tile);
            }
        }
        else // When the tile should show for everyone
        {
            // Adjust the logic for tileIndex retrieval based on your current structure
            TileBase tile = tileIndex >= 0 && tileIndex < m_objectTileArray[m_currentSelectedStructure].Length
                ? m_objectTileArray[m_currentSelectedStructure][tileIndex]
                : null;

            m_TilemapArray[tilemapindex].SetTile(position, tile);
            m_TilemapArray[tilemapindex].SetTileFlags(position, TileFlags.None);

            if (isopaque)
            {
                m_TilemapArray[tilemapindex].SetColor(position, new Color(0, 0, 0, 0.3f)); // Set the sprite to be transparent
            }
            else
            {
                m_TilemapArray[tilemapindex].SetColor(position, Color.white); // Make the sprite a solid color
            }
        }

        // Ensure that the update happens across clients as well
        UpdateTileOnClientsRpc(position, tileIndex, networkObjectId, tilemapindex, justOwner, isopaque);
    }


    [Rpc(SendTo.NotServer)]
    void UpdateTileOnClientsRpc(Vector3Int position, int tileIndex, ulong networkObjectId, int tilemapindex, bool justOwner, bool isOpaque)
    {
        if (justOwner) // When the tile should show for just the owner
        {
            if (OwnerClientId == networkObjectId && IsOwner)
            {
                // Make sure we get the correct tile from the selected structure
                TileBase tile = tileIndex >= 0 && tileIndex < m_objectTileArray[m_currentSelectedStructure].Length
                    ? m_objectTileArray[m_currentSelectedStructure][tileIndex]
                    : null;

                if (m_TilemapArray[tilemapindex] != null)
                {
                    m_TilemapArray[tilemapindex].SetTile(position, tile);
                }
            }
        }
        else // When the tile should show for everyone
        {
            // Adjust the tile retrieval logic to use m_objectArray
            TileBase tile = tileIndex >= 0 && tileIndex < m_objectTileArray[m_currentSelectedStructure].Length
                ? m_objectTileArray[m_currentSelectedStructure][tileIndex]
                : null;

            if (m_TilemapArray[tilemapindex] != null)
            {
                m_TilemapArray[tilemapindex].SetTile(position, tile);
                m_TilemapArray[tilemapindex].SetTileFlags(position, TileFlags.None);

                // Set whether the tile is opaque
                if (isOpaque)
                {
                    m_TilemapArray[tilemapindex].SetColor(position, new Color(0, 0, 0, 0.3f)); // Set the sprite to be transparent
                }
                else
                {
                    m_TilemapArray[tilemapindex].SetColor(position, Color.white); // Make the sprite a solid color
                }
            }
        }
    }


    private int GetTileIndex(TileBase tile)
    {
        // Iterate through the selected structure's tile array
        TileBase[] selectedStructureTiles = m_objectTileArray[m_currentSelectedStructure];
        for (int i = 0; i < selectedStructureTiles.Length; i++)
        {
            if (selectedStructureTiles[i] == tile)
            {
                return i;
            }
        }
        return -1; // Return -1 if the tile is not found
    }

    private int GetTilemapIndex(Tilemap tilemap)
    {
        for (int i = 0; i < m_TilemapArray.Length; i++)
        {
            if (m_TilemapArray[i] == tilemap)
            {
                return i;
            }
        }
        return -1;
    }

    public bool GetCanBuild()
    {
        return m_canBuild;
    }

    public bool GetIsBuilding()
    {
        return m_isBuilding;
    }

    // Debug function for drawing gizmos of the players's build range
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, m_buildDistance);
    }
}
