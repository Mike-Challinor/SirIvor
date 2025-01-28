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
    private float m_buildDistance = 1.5f;
    private TileManager m_tileManager;

    [SerializeField] private bool m_isBuilding = false;
    [SerializeField] private bool m_startBuilding = false;
    [SerializeField] private bool m_canBuild = false;
    [SerializeField] private bool m_isBuildMode = false;
    [SerializeField] private TileBase[] m_TileArray;
    [SerializeField] private Tilemap[] m_TilemapArray;
    [SerializeField] private GameObject m_buildSprite;
    [SerializeField] private GameObject m_playerHUDLocal;

    private float lastUpdateTime = 0f;
    private const float updateInterval = 0.05f; // 50ms interval for updates
    private Dictionary<Vector3Int, TileBase> localTileCache = new Dictionary<Vector3Int, TileBase>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();

        if (m_TileArray.Length > 0)
        {
            m_equippedTile = m_TileArray[0];
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

            // Check if the position has changed or the cache doesn't match
            if (cellPosition != m_buildTileLocation || !localTileCache.ContainsKey(cellPosition))
            {
                // Remove the previous tile immediately
                if (localTileCache.ContainsKey(m_buildTileLocation))
                {
                    m_buildModeTilemap.SetTile(m_buildTileLocation, null);
                    localTileCache.Remove(m_buildTileLocation); // Clear cache for the old location
                }

                // Update the build location
                m_buildTileLocation = cellPosition;

                // Set a new tile if equipped
                if (m_equippedTile != null)
                {
                    TileBase tile = m_equippedTile;

                    // Update locally
                    m_buildModeTilemap.SetTile(m_buildTileLocation, tile);
                    localTileCache[m_buildTileLocation] = tile;

                    // Sync with the server
                    SetTileOnServerRpc(m_buildTileLocation, GetTileIndex(m_equippedTile), OwnerClientId, GetTilemapIndex(m_buildModeTilemap), true, true);
                }
                else
                {
                    Debug.Log("Equipped tile is null");
                }

                // Check if the tile can be built at the location
                if (m_structuresTilemap != null)
                {
                    TileBase structureTile = m_structuresTilemap.GetTile(cellPosition);
                    m_buildModeTilemap.color = structureTile == null ? Color.white : Color.red;
                    m_canBuild = structureTile == null ? true : false;
                }
                else
                {
                    m_canBuild = false;
                    Debug.Log("PLAYERCONTROLLERBUILDER::UPDATE:: Structures tilemap is null");
                }
            }
            else
            {
                // Force synchronization if a desync is detected
                SyncTilesWithServer();
            }
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

            // Sync with the server
            Debug.Log($"Tilemap index is: " + GetTilemapIndex(m_structuresTilemap));
            Debug.Log($"Tile index is: " + GetTileIndex(m_equippedTile));
            SetTileOnServerRpc(m_buildTileLocation, GetTileIndex(m_equippedTile), OwnerClientId, GetTilemapIndex(m_structuresTilemap), false, true);

        }

        else
        {
            Debug.Log("PLAYERCONTROLLERBUILDER::STARTBUILD:: Player is already building");
        }

    }

    private IEnumerator BuildingTimer()
    {
        Debug.Log("PLAYERCONTROLLER::BUILDINGTIMER:: Function called...");

        // Add the tile to the tile manager
        m_tileManager.AddSingleSprite(m_buildTileLocation, 0, GetMaxHealthOfTileToBuild(), m_tileManager.GetTileTypeFromArrays(m_equippedTile)); // Set starting health to 0
        
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

        // Set the player hud to active
        m_playerHUDLocal.SetActive(true);

        // Continue updating health until it reaches max health
        while (tileData.CurrentHealth < tileData.MaxHealth)
        {
            float remainingHealth = tileData.MaxHealth - tileData.CurrentHealth;

            // Add either the increment or the remaining health, whichever is smaller
            float healthToAdd = Mathf.Min(healthIncrement, remainingHealth);
            m_tileManager.SetTileHealth(m_buildTileLocation, healthToAdd);

            // Get tiles health values
            float? currentHealth = m_tileManager.GetTileHealth(m_buildTileLocation);
            float? maxHealth = m_tileManager.GetMaxHealth(m_buildTileLocation);

            float percentage = (currentHealth.Value / maxHealth.Value); // Percentage between 0 and 1 for slider

            if (currentHealth.HasValue) 
            {
                m_playerHUDLocal.GetComponentInChildren<Slider>().value = percentage; 
            }

            Debug.Log($"Added {healthToAdd} health to tile at {m_buildTileLocation}. Current health: {tileData.CurrentHealth}");

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

        // Call the server to set the tile
        SetTileOnServerRpc(m_buildTileLocation, GetTileIndex(m_equippedTile), OwnerClientId, GetTilemapIndex(m_structuresTilemap), false, false);
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
                TileBase tile = tileIndex >= 0 && tileIndex < m_TileArray.Length ? m_TileArray[tileIndex] : null;
                m_TilemapArray[tilemapindex].SetTile(position, tile);
            }
        }

        else // When the tile should show for everyone
        {
            TileBase tile = tileIndex >= 0 && tileIndex < m_TileArray.Length ? m_TileArray[tileIndex] : null;
            m_TilemapArray[tilemapindex].SetTile(position, tile);
            m_TilemapArray[tilemapindex].SetTileFlags(position, TileFlags.None);

            if (isopaque)
            {
                m_TilemapArray[tilemapindex].SetColor(position, new Color(0, 0, 0, 0.3f)); // Set the sprite to be transparent
            }

            else
            {
                m_TilemapArray[tilemapindex].SetColor(position, Color.white); // Make the sprite a solid colour
            }
        }
        

        UpdateTileOnClientsRpc(position, tileIndex, networkObjectId, tilemapindex, justOwner, isopaque);
    }

    [Rpc(SendTo.NotServer)]
    void UpdateTileOnClientsRpc(Vector3Int position, int tileIndex, ulong networkObjectId, int tilemapindex, bool justOwner, bool isOpaque)
    {
        if (justOwner) // When the tile should show for just the owner
        {
            if (OwnerClientId == networkObjectId && IsOwner)
            {
                TileBase tile = tileIndex >= 0 && tileIndex < m_TileArray.Length ? m_TileArray[tileIndex] : null;

                if (m_TilemapArray[tilemapindex] != null)
                {
                    m_TilemapArray[tilemapindex].SetTile(position, tile);
                }
            }
        }

        else // When the tile should show for everyone
        {
            TileBase tile = tileIndex >= 0 && tileIndex < m_TileArray.Length ? m_TileArray[tileIndex] : null;

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
                    m_TilemapArray[tilemapindex].SetColor(position, Color.white); // Make the sprite a solid colour
                }
            }

        }
        
    }

    private int GetTileIndex(TileBase tile)
    {
        for (int i = 0; i < m_TileArray.Length; i++)
        {
            if (m_TileArray[i] == tile)
            {
                return i;
            }
        }
        return -1;
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
