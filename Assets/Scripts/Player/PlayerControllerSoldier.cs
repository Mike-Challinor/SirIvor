using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerControllerSoldier : PlayerController
{
    private bool m_shootMode = false;
    [SerializeField] private bool m_canInteract = false;
    private float m_interactDistance = 1f;
    private Tilemap m_structuresTilemap;
    private TileManager m_tileManager;
    private Vector3 m_returnPosition;
    private Vector3 m_platformPosition;

    [SerializeField] TileBase[] m_platformArray;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();

        // Get references
        m_structuresTilemap = GameObject.FindWithTag("StructuresTilemap").GetComponent<Tilemap>();
        m_tileManager = GameObject.FindWithTag("Tilemanager").GetComponent<TileManager>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (!IsOwner) { return; }

        // Check if near to a platform

        // Logic when in shoot mode
        if (m_shootMode)
        {

        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("StructuresTilemap"))
        {
            Debug.Log("Near to a structure");


            // Get the collision point
            Vector2 collisionPoint = collision.ClosestPoint(transform.position);

            // Convert world position to tilemap cell position
            Vector3Int collidedTile = m_structuresTilemap.WorldToCell(collisionPoint);
            Debug.Log($"Collided tile position: {collidedTile}");

            // Check if the tile belongs to a platform
            TileBase tile = m_structuresTilemap.GetTile(collidedTile);
            if (tile != null && m_tileManager.GetTileTypeFromArrays(tile) == "Platform")
            {
                if (!m_canInteract)
                {
                    m_canInteract = true;
                }

                Vector3Int cellPosition;
                Vector3 position = new Vector3(0, 0, 0);

                for (int i = 0; i < 4; i++)
                {
                    if (m_platformArray[i] == m_structuresTilemap.GetTile(collidedTile) || m_platformArray[i + 4] == m_structuresTilemap.GetTile(collidedTile))
                    {
                        switch (i)
                        {
                            case 0: //Bottom left tile
                                cellPosition = new Vector3Int(collidedTile.x, collidedTile.y + 1, 0);
                                position = m_structuresTilemap.CellToWorld(cellPosition);
                                break;

                            case 1: //Bottom right tile
                                cellPosition = new Vector3Int(collidedTile.x - 1, collidedTile.y + 1, 0);
                                position = m_structuresTilemap.CellToWorld(cellPosition);
                                break;

                            case 2: //Top left tile
                                cellPosition = collidedTile;
                                position = m_structuresTilemap.CellToWorld(cellPosition);
                                break;

                            case 3: //Top right tile
                                cellPosition = new Vector3Int(collidedTile.x + 1, collidedTile.y, 0);
                                position = m_structuresTilemap.CellToWorld(cellPosition);
                                break;

                        }
                    }
                }

                // Convert tile position to world position and adjust player position
                m_platformPosition = position + new Vector3(0.5f, 0.5f, 0);
                Debug.Log($"Moving player to: {m_platformPosition}");

            }

        }
    }


    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("StructuresTilemap"))
        {
            Debug.Log("Left structure range");

            // Get the closest collision point
            Vector2 collisionPoint = collision.ClosestPoint(transform.position);

            // Convert the world position to a tile cell position
            Vector3Int cellPosition = m_structuresTilemap.WorldToCell(collisionPoint);

            TileBase tile = m_structuresTilemap.GetTile(cellPosition);

            if (tile != null && m_tileManager.GetTileTypeFromArrays(tile) == "Platform")
            {
                if (m_canInteract)
                {
                    m_canInteract = false;
                }

                Debug.Log(" Structure was a platform");
            }
        }
    }

    public void Shoot()
    {
        Debug.Log("Shoot");
    }

    public void SetShootMode()
    {
        if (m_shootMode)
        {
            Debug.Log("Moving player to the platforms position");
            transform.position = m_returnPosition;
        }

        else
        {
            m_canInteract = false;
            m_returnPosition = transform.position;
            transform.position = m_platformPosition; 

            Debug.Log("Moving player from the platforms position");
        }

        m_shootMode = !m_shootMode;

    }

    public bool GetInteractStatus()
    {
        return m_canInteract;
    }

    public bool GetShootStatus()
    {
        return m_shootMode;
    }

    public void PositionPlayerOnPlatform()
    {
        
    }

    // Debug function for drawing gizmos of the players's build range
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, m_interactDistance);
    }
}
