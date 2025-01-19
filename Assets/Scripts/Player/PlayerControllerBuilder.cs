using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class PlayerControllerBuilder : PlayerController
{
    private bool m_isBuilding = false;
    private float m_buildTimer = 1f;
    private GameObject m_buildModeGameObject;
    private TileBase m_equippedTile;
    private Tilemap m_buildModeTilemap;
    private Tilemap m_structuresTilemap;
    private Vector3Int m_buildTileLocation;
    private bool m_canBuild = false;

    [SerializeField] private bool m_isBuildMode = false;
    [SerializeField] private TileBase[] m_TileArray;
    [SerializeField] private GameObject m_buildSprite;

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
        }

        m_structuresTilemap = GameObject.FindWithTag("StructuresTilemap").GetComponent<Tilemap>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (!IsOwner) return;

        if (m_isBuildMode)
        {
            if (m_playerCamera == null)
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
                            SetTileOnServerRpc(m_buildTileLocation, GetTileIndex(m_equippedTile), OwnerClientId);
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
                        }
                        else
                        {
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
        }
    }

    public void SetBuildMode()
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
        SetTileOnServerRpc(m_buildTileLocation, -1, OwnerClientId);
    }

    private IEnumerator StartBuild()
    {
        if (!m_isBuilding)
        {
            m_isBuilding = true;
            yield return StartCoroutine(BuildingTimer());
        }
        else
        {
            Debug.Log("PLAYERCONTROLLERBUILDER::STARTBUILD:: Player is already building");
        }

        CancelBuilding();
    }

    private IEnumerator BuildingTimer()
    {
        yield return new WaitForSeconds(m_buildTimer);
    }

    public void CancelBuilding()
    {
        m_isBuilding = false;
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
        Debug.Log("PLAYERCONTROLLERBUILDER::SYNCTILESWITHSERVER:: Synchronizing tiles with server");

        // Request the server to resend the current state of the tilemap
        RequestTileSyncServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestTileSyncServerRpc()
    {
        Debug.Log("PLAYERCONTROLLERBUILDER::REQUESTTILESYNC:: Server received sync request");

        // Iterate through all tiles in the current state
        foreach (var position in localTileCache.Keys)
        {
            TileBase tile = localTileCache[position];
            int tileIndex = GetTileIndex(tile);
            UpdateTileOnClientsRpc(position, tileIndex, OwnerClientId);
        }
    }

    [Rpc(SendTo.Server)]
    void SetTileOnServerRpc(Vector3Int position, int tileIndex, ulong networkObjectId)
    {
        if (OwnerClientId == networkObjectId && IsOwner)
        {
            TileBase tile = tileIndex >= 0 && tileIndex < m_TileArray.Length ? m_TileArray[tileIndex] : null;
            m_buildModeTilemap.SetTile(position, tile);
        }

        UpdateTileOnClientsRpc(position, tileIndex, networkObjectId);
    }

    [Rpc(SendTo.NotServer)]
    void UpdateTileOnClientsRpc(Vector3Int position, int tileIndex, ulong networkObjectId)
    {
        if (OwnerClientId == networkObjectId && IsOwner)
        {
            TileBase tile = tileIndex >= 0 && tileIndex < m_TileArray.Length ? m_TileArray[tileIndex] : null;

            if (m_buildModeTilemap != null)
            {
                m_buildModeTilemap.SetTile(position, tile);
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
}
