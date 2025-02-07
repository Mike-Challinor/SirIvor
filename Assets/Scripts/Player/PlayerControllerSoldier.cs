using UnityEngine;
using Unity.Netcode;
using UnityEngine.Tilemaps;
using System.Collections;
using System.IO.Pipes;
using UnityEngine.UIElements;

public class PlayerControllerSoldier : PlayerController
{
    private float m_interactDistance = 1f;
    private Tilemap m_structuresTilemap;
    private TileManager m_tileManager;
    private Vector3 m_returnPosition;
    private Vector3 m_platformPosition;
    private Vector2 m_lastDirection = Vector2.right; // Default to right facing
    private Color m_gizmoColour = Color.red;


    [SerializeField] private TileBase[] m_platformArray;
    [SerializeField] private GameObject m_projectilePrefab;
    [SerializeField] private GameObject m_firePoint;
    [SerializeField] private GameObject m_gunPosition;

    [SerializeField] private bool m_canInteract = false;
    [SerializeField] private bool m_shootMode = false;
    [SerializeField] private bool m_isAttacking = false;
    [SerializeField] private bool m_canAttack = false;

    [SerializeField] private float m_attackTimer = 0.5f;

    NetworkVariable<bool> m_networkGunEnabled = new NetworkVariable<bool>(
        value: false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    NetworkVariable<Vector3> m_networkGunPosition = new NetworkVariable<Vector3>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        m_structuresTilemap = GameObject.FindWithTag("StructuresTilemap")?.GetComponent<Tilemap>();
        m_tileManager = GameObject.FindWithTag("Tilemanager")?.GetComponent<TileManager>();

        m_networkGunEnabled.OnValueChanged += Handle_NetworkGunEnabled_OnValueChanged;
        m_networkGunPosition.OnValueChanged += Handle_NetworkGunPosition_OnValueChanged;

    }

    protected override void Update()
    {
        base.Update();

        if (!IsOwner) return;

        UpdateMovementDirection(); // Update movement direction for line trace

        CheckForPlatform(); // Check for platform to interact with

        if (m_shootMode && base.IsMouseWithinScreen() && Application.isFocused) // Track mouse position when in shoot mode
        {
            if (!m_playerHUD.GetReticleStatus()) // Show the players reticle
            {
                m_playerHUD.SetReticleStatus(true);
            }

            m_playerHUD.SetReticlePosition(Input.mousePosition);

            SetFirepointPosition();

        }

        else
        {
            if (m_playerHUD.GetReticleStatus())
            {
                m_playerHUD.SetReticleStatus(false);
            }
        }
    }

    private void UpdateMovementDirection()
    {
        Vector2 inputDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (inputDirection != Vector2.zero)
        {
            m_lastDirection = inputDirection.normalized;
        }
    }

    private void SetFirepointPosition()
    {
        Vector3 direction = (GetMousePos() - transform.position).normalized;
        Vector3 newFirePointPosition = transform.position + (direction * 0.8f);

        // Update local firePoint
        m_firePoint.transform.position = newFirePointPosition;

        // Rotate firePoint to face the mouse direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        m_firePoint.transform.rotation = Quaternion.Euler(0, 0, angle);

        if (direction.x > 0)
        {
            m_gunPosition.GetComponent<SpriteRenderer>().flipX = false; // Flip sprite locally

            // Update position on the network variable
            Vector3 position = new Vector3(transform.position.x + 0.5f, transform.position.y, 0);
            SetGunPositionRpc(position); 
            
        }
        else if (direction.x < 0)
        {
            m_gunPosition.GetComponent<SpriteRenderer>().flipX = true; // Flip sprite locally

            // Update position on the network variable
            Vector3 position = new Vector3(transform.position.x - 0.5f, transform.position.y, 0);
            SetGunPositionRpc(position); 
        }
    }

    private void CheckForPlatform()
    {
        int layerMask = ~LayerMask.GetMask("Player"); // Ignore Player layer

        RaycastHit2D hit = Physics2D.Raycast(transform.position, m_lastDirection, m_interactDistance, layerMask);

        if (hit.collider != null && hit.collider.CompareTag("StructuresTilemap"))
        {
            Vector3Int tilePosition = m_structuresTilemap.WorldToCell(hit.point);
            TileBase tile = m_structuresTilemap.GetTile(tilePosition);

            if (tile != null && m_tileManager.GetTileTypeFromArrays(tile) == "Platform" && m_canInteract == false)
            {
                m_canInteract = true;
                m_gizmoColour = Color.green;
                m_platformPosition = GetPlatformPosition(tilePosition);
                Debug.Log($"Near a platform, setting interaction: {m_canInteract}");
            }
        }

        else
        {
            if (m_canInteract == true)
            {
                m_canInteract = false;
                Debug.Log($"Can no longer interact with platform, setting interaction: {m_canInteract}");
                m_gizmoColour = Color.red;
            }
        }
    }


    private Vector3 GetPlatformPosition(Vector3Int tilePosition)
    {
        Debug.Log($"Tile position = {tilePosition}");
        for (int i = 0; i < 4; i++)
        {
            if (m_platformArray[i] == m_structuresTilemap.GetTile(tilePosition) || m_platformArray[i + 4] == m_structuresTilemap.GetTile(tilePosition))
            {
                Vector3Int newCellPos = i switch
                {
                    0 => new Vector3Int(tilePosition.x, tilePosition.y + 1, 0),
                    1 => new Vector3Int(tilePosition.x - 1, tilePosition.y + 1, 0),
                    2 => tilePosition,
                    3 => new Vector3Int(tilePosition.x - 1, tilePosition.y, 0),
                    _ => tilePosition
                };

                Debug.Log($"Cell position = {newCellPos}");

                return m_structuresTilemap.CellToWorld(newCellPos) + new Vector3(1f, 0.5f, 0);
            }
        }

        return tilePosition;
    }

    public void SetShootMode()
    {
        if (m_shootMode)
        {
            transform.position = m_returnPosition; // Set the return position for the player
            SetGunStatusRpc(false); // Hide the players gun sprite
        }
        else
        {
            m_RB.linearVelocity = Vector2.zero; // Zero the players velocity
            SetGunStatusRpc(true); // Show the players gun sprite
            m_canAttack = true;
            m_returnPosition = transform.position; // Set the position for the player to return to when exiting shoot mode
            transform.position = m_platformPosition; // Set players position to the platforms position
        }

        m_shootMode = !m_shootMode;
    }

    public void InitiateAttack()
    {
        Debug.Log("Initiate Attack function called...");
        
        if (base.IsMouseWithinScreen())
        {
            Debug.Log("Mouse is within screen...");
            m_isAttacking = true;
            StartCoroutine(Attack());
        }

        else
        {
            Debug.LogError("Mouse is not within screen");
        }
    }

    private IEnumerator Attack()
    {
        Debug.Log("Attack function called...");

        while (m_isAttacking)
        {
            SpawnProjectile();

            // Set whether the player can attack
            m_canAttack = false;

            // Start and wait for attack timer
            yield return StartCoroutine(AttackTimer());

        }
    }

    private IEnumerator AttackTimer()
    {
        yield return new WaitForSeconds(m_attackTimer);
        m_canAttack = true;
    }

    public void EndAttack()
    {
        m_isAttacking = false;
    }

    public bool GetCanAttack()
    {
        return m_canAttack;
    }

    private void SpawnProjectile()
    {
        Debug.Log("Spawn projectile function called");

        Vector2 fireDirection = GetFireDirection(m_firePoint.transform.position);
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;

        SpawnProjectileRpc(fireDirection, m_firePoint.transform.position, m_firePoint.transform.rotation);

    }

    [Rpc(SendTo.Server)]
    private void SpawnProjectileRpc(Vector3 direction, Vector3 position, Quaternion rotation)
    {
        GameObject projectile = Instantiate(m_projectilePrefab, position, rotation);
        Projectile projectileComponent = projectile.GetComponent<Projectile>();

        if (projectileComponent == null)
        {
            Debug.LogError("Projectile component is missing!");
            return;
        }

        else
        {
            projectileComponent.SetDirection(direction);
        }

        projectile.GetComponent<NetworkObject>().Spawn();
    }

    [Rpc(SendTo.Server)]
    private void SetGunStatusRpc(bool isActive)
    {
        m_networkGunEnabled.Value = isActive;
    }

    private void Handle_NetworkGunEnabled_OnValueChanged(bool previousValue, bool newValue)
    {
        m_gunPosition.SetActive(newValue);
    }

    [Rpc(SendTo.Server)]
    private void SetGunPositionRpc(Vector3 position)
    {
        m_networkGunPosition.Value = position;
    }

    private void Handle_NetworkGunPosition_OnValueChanged(Vector3 previousValue, Vector3 newValue)
    {
        m_gunPosition.transform.position = newValue;

        Debug.Log($"Network gun position has changed to {newValue} on {GetComponent<NetworkObject>().NetworkObjectId} ");

        if (m_gunPosition.transform.position.x > 0)
        {
            m_gunPosition.GetComponent<SpriteRenderer>().flipX = false;
        }

        else if (m_gunPosition.transform.position.x < 0)
        {
            m_gunPosition.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    private Vector3 GetMousePos()
    {
        // Get mouse position
        Vector3 mousePos = m_playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0; // Ensure z is at the same level as the player

        return mousePos;
    }

    private Vector3 GetFireDirection(Vector3 startPos)
    {
        // Calculate direction to mouse
        Vector3 direction = (GetMousePos() - startPos).normalized;

        return direction;
    }

    public bool GetInteractStatus() => m_canInteract;
    public bool GetShootStatus() => m_shootMode;

    private void OnDrawGizmos()
    {
        Gizmos.color = m_gizmoColour;
        Vector3 endPosition = transform.position + (Vector3)m_lastDirection * m_interactDistance;
        Gizmos.DrawLine(transform.position, endPosition);
    }
}