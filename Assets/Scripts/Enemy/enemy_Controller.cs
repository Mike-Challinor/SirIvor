using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class enemy_Controller : NetworkBehaviour
{
    [SerializeField] private GameObject[] m_allTargets;
    [SerializeField] private float m_moveSpeed = 1f;
    [SerializeField] private float m_attackDamage = 20f;
    [SerializeField] private float m_attackRange = 2f;
    [SerializeField] private float m_attackDuration = 1f;
    [SerializeField] private float m_attackCooldown = 2f;
    [SerializeField] private float m_aggroDistance = 15f;
    [SerializeField] private Vector3 m_target;
    [SerializeField] private bool m_hasTarget = false;
    [SerializeField] private BoxCollider2D m_attackCollider;
    [SerializeField] private bool m_isAttacking = false;

    private NavMeshAgent m_navMeshAgent;
    private TileManager m_tileManager;

    private bool m_isFacingRight = false;
    private SpriteRenderer m_enemySprite;

    private Rigidbody2D m_RB;
    private HealthComponent m_healthComponent;
    private PlayerHUD m_playerHUD;

    private NetworkVariable<Vector2> m_networkPosition = new NetworkVariable<Vector2>(
        default, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_RB = GetComponent<Rigidbody2D>();
        m_healthComponent = GetComponent<HealthComponent>();
        m_enemySprite = GetComponent<SpriteRenderer>();
        m_playerHUD = GetComponent<PlayerHUD>();
        m_tileManager = GameObject.FindGameObjectWithTag("Tilemanager").GetComponent<TileManager>();

        // Initialise the nav mesh agent and updates 
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_navMeshAgent.updateRotation = false;
        m_navMeshAgent.updateUpAxis = false;

        // Find the nearest fence tile
        List<Vector3Int> allTargets = new List<Vector3Int>();
        allTargets.AddRange(m_tileManager.GetFencePositions());
        m_target = FindNearestTarget(allTargets);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        // Reset move direction
        Vector2 moveDir = Vector2.zero;

        // Look for a target if the enemy does not have one
        if (m_hasTarget == false)
        {
            Debug.Log("ENEMY_CONTROLLER::UPDATE:: Enemy has no target..  Calling FindTarget() function...");
            FindTarget();
        }
        else
        {
            // Find a new target if the health of the target is 0 or less
            if (m_tileManager != null)
            {
                if (m_tileManager.GetTileHealth(m_tileManager.GetTilemap().WorldToCell(m_target)) <= 0)
                {
                    m_hasTarget = false;
                    return;
                }
            }

            // Set the enemy's move direction towards the target
            moveDir = m_target - transform.position;

            // Normalize the move direction
            moveDir.Normalize();

            // Move the attack collider based on targets direction
            MoveAttackColliderRpc(moveDir); // Call the RPC to update the attack collider's offset

            // Calculate distance from target
            float distance = Vector2.Distance(m_target, transform.position);

            if (distance > m_attackRange)
            {
                // Move the enemy towards the target
                m_navMeshAgent.SetDestination(m_target);

                if ((moveDir.x > 0 && !m_isFacingRight) || (moveDir.x < 0 && m_isFacingRight))
                {
                    // If sprite is facing the wrong way, flip the sprite
                    FlipSpriteRpc();
                }
            }
            else
            {
                // Initiate enemy attack if not already attacking
                if (!m_isAttacking)
                {
                    Debug.Log("ENEMY_CONTROLLER::UPDATE:: Enemy is within range of target. Calling InitiateAttack Function...");
                    StartCoroutine(InitiateAttack());
                }
            }
        }

        // Synchronize position with all clients
        if (m_networkPosition.Value != (Vector2)transform.position)
        {
            m_networkPosition.Value = transform.position;
            UpdateEnemyPositionRpc(transform.position); // Call ClientRpc for position sync
        }
    }

    // Function for finding the enemy's target
    private void FindTarget()
    {
        Debug.Log("ENEMY_CONTROLLER::FINDTARGET:: Finding enemy target...");

        // Find the nearest tile to the main structure
        List<Vector3Int> allTargets = new List<Vector3Int>();

        allTargets.AddRange(m_tileManager.GetBuildingPositions());

        if (allTargets == null)
        {
            Debug.Log("All targets is null");
        }

        else
        {
            Debug.Log("All targets is not null");
        }

        m_target = FindNearestTarget(allTargets);

        // Check if path is valid
        if (HasValidPath(m_target))
        {
            Debug.Log("Path to building is valid");
            return;
        }

        else
        {
            Debug.Log("Path to building is not valid... finding fence");

            allTargets.Clear();

            // Find the nearest tile to the fences structure
            allTargets.AddRange(m_tileManager.GetFencePositions());
            m_target = FindNearestTarget(allTargets);
        }
    }

    private Vector3 FindNearestTarget(List<Vector3Int> allTargets)
    {
        // Reset the temporary target variable
        Vector3Int tempTarget = new Vector3Int();

        if (allTargets == null)
        {
            return new Vector3(0, 0, 0);
        }

        // Remove any buildings that are too far away or dead
        allTargets.RemoveAll(tile =>
        {
            // Use sqrMagnitude to get float distance
            float targetDistance = (m_tileManager.GetTilemap().CellToWorld(tile) - transform.position).sqrMagnitude; // Get the world posiiton and calculate distance
            targetDistance = Mathf.Sqrt(targetDistance);
            return targetDistance > m_aggroDistance || m_tileManager.GetTileHealth(tile) <= 0; // Remove if tile is dead or too far away

            

        });

        // Loop through all potential targets
        foreach (Vector3Int target in allTargets)
        {
            // If this is the first object in the list, set it as the temp target
            if (tempTarget == null)
            {
                tempTarget = target;
            }
            else
            {
                // Use sqrMagnitude to get float distances
                float tempTargetDistance = (m_tileManager.GetTilemap().CellToWorld(tempTarget) - transform.position).sqrMagnitude; // Get the world posiiton of temp target and calculate distance
                float foundTargetDistance = (m_tileManager.GetTilemap().CellToWorld(target) - transform.position).sqrMagnitude; // Get the world posiiton of found target and calculate distance

                // Check if the found target's distance is less than the current temp target
                if (foundTargetDistance < tempTargetDistance)
                {
                    // Update the temp target to the closer found target
                    tempTarget = target;
                }
            }
        }

        // Convert the temp target from cell position to its world position and store in chosenTarget (Vector3)
        Vector3 chosenTarget = m_tileManager.GetTilemap().CellToWorld(tempTarget);

        m_hasTarget = true;

        Debug.Log($"Target found at cell position: {tempTarget}");
        Debug.Log($"Converted target to world position: {chosenTarget}");

        // Return the chosen target
        return chosenTarget;
    }

    // Begin attack which calls the attack timer
    private IEnumerator InitiateAttack()
    {
        m_isAttacking = true;
        m_enemySprite.color = Color.red;
        Debug.Log("ENEMY_CONTROLLER::INITIATEATTACK:: Initiating attack. Calling attack timer..");
        yield return StartCoroutine(AttackTimer()); // Wait for attack timer to finish before continuing
        Debug.Log("ENEMY_CONTROLLER::INITIATEATTACK:: Calling AttackTarget() function..");
        AttackTarget();
    }

    // Attack timer that signifies how long the attack/animation takes
    private IEnumerator AttackTimer()
    {
        Debug.Log("ENEMY_CONTROLLER::ATTACKTIMER:: Function called. Waiting for attack duration");
        yield return new WaitForSeconds(m_attackDuration);
        Debug.Log("ENEMY_CONTROLLER::ATTACKTIMER:: Attack timer ended.. attacking target");
    }

    // Attack the target once the timer has ended
    private void AttackTarget()
    {
        Debug.Log("ENEMY_CONTROLLER::ATTACKTARGET:: Function called");

        // Define the attack area based on the collider's center
        Vector2 attackCenter = m_attackCollider.bounds.center;
        Vector2 attackSize = new Vector2(m_attackCollider.bounds.size.x, m_attackCollider.bounds.size.y);

        // Call the ServerRpc to handle attack logic on the server
        AttackTargetRpc(attackCenter, attackSize);
    }

    // ServerRpc for handling the attack logic on the server
    [Rpc(SendTo.Server)]
    private void AttackTargetRpc(Vector2 attackCenter, Vector2 attackSize)
    {
        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(attackCenter, attackSize, 0);

        foreach (var collider in hitColliders)
        {            
            if (collider.CompareTag("StructuresTilemap")) // If the collider belongs to a structure
            {
                // Check if tile is in a group (is a building) or not (is a fence)
                if (m_tileManager.IsTileInGroup(m_tileManager.GetTilemap().WorldToCell(m_target)))
                {
                    // Update tile group health on server
                    m_tileManager.UpdateTileGroupHealth(m_tileManager.GetTileGroup(m_tileManager.GetTilemap().WorldToCell(m_target)), -m_attackDamage);

                    // Update tile group health on clients
                    UpdateTileGroupHealthClientRpc(m_tileManager.GetTilemap().WorldToCell(m_target));

                }

                else
                {
                    // Remove health from the tile on the server
                    m_tileManager.RemoveTileHealth(m_tileManager.GetTilemap().WorldToCell(m_target), m_attackDamage);

                    // Check to see if the tile has been destroyed
                    if (m_tileManager.GetTileHealth(m_tileManager.GetTilemap().WorldToCell(m_target)) == 0)
                    {
                        // Reset target so a new target is found
                        m_hasTarget = false;
                    }

                    // Update tile health on clients
                    UpdateTileHealthClientRpc(m_target);
                }
            }
        }

        // Call attack cooldown after applying damage
        StartCoroutine(AttackCooldown());
    }

    // ClientRpc to update health of tile on all clients
    [Rpc(SendTo.NotServer)]
    private void UpdateTileHealthClientRpc(Vector3 target)
    {
        // Remove health from the tile
        m_tileManager.RemoveTileHealth(m_tileManager.GetTilemap().WorldToCell(target), m_attackDamage);

        // Check to see if the tile has been destroyed
        if (m_tileManager.GetTileHealth(m_tileManager.GetTilemap().WorldToCell(m_target)) == 0)
        {
            // Reset target so a new target is found
            m_hasTarget = false;
        }
    }

    // ClientRpc to update health on all clients
    [Rpc(SendTo.NotServer)]
    private void UpdateTileGroupHealthClientRpc(Vector3Int target)
    {
        UpdateTileGroupHealth(target);
    }

    private void UpdateTileGroupHealth(Vector3Int target)
    {
        m_tileManager.UpdateTileGroupHealth(m_tileManager.GetTileGroup(target), m_attackDamage);
    }

    // ClientRpc to update the enemy's position on all clients
    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateEnemyPositionRpc(Vector2 newPosition)
    {
        transform.position = newPosition;
    }

    // Timer for attack cooldown (time it takes between attacks)
    private IEnumerator AttackCooldown()
    {
        Debug.Log("ENEMY_CONTROLLER::ATTACKCOOLDOWN:: Ending Attack!");
        ChangeSpriteColour(Color.white); // Return sprite to white colour
        yield return new WaitForSeconds(m_attackCooldown);
        m_isAttacking = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void MoveAttackColliderRpc(Vector2 moveDir)
    {
        // Calculate the new collider offset based on the direction
        Vector2 newOffset = moveDir * (m_attackRange / 2);
        m_attackCollider.offset = newOffset;
    }

    // Function for flipping the sprite
    [Rpc(SendTo.ClientsAndHost)]
    void FlipSpriteRpc()
    {
        m_isFacingRight = !m_isFacingRight;
        m_enemySprite.flipX = !m_enemySprite.flipX;
    }

    bool HasValidPath(Vector3 targetPosition)
    {
        NavMeshPath path = new NavMeshPath();
        if (m_navMeshAgent.CalculatePath(targetPosition, path))
        {
            return path.status == NavMeshPathStatus.PathComplete;
        }
        return false;
    }

    // Debug function for drawing gizmos of the enemy's attack size
    private void OnDrawGizmos()
    {
        if (m_attackCollider != null)
        {
            // Use the collider's center, width, and height for the Gizmo
            Vector2 attackCenter = m_attackCollider.bounds.center;
            Vector2 attackSize = new Vector2(m_attackCollider.bounds.size.x, m_attackCollider.bounds.size.y);

            Gizmos.DrawWireCube(attackCenter, (Vector3)attackSize); // Cast to Vector3 for visualization
            
        }

        Gizmos.DrawWireSphere(transform.position, m_aggroDistance);
    }

    void ChangeSpriteColour(Vector4 colour)
    {
        m_enemySprite.color = colour;
        Debug.Log("$ENEMYCONTROLLER::CHANGESPRITECOLOURRPC:: Colour of sprite changed to: " + colour);
    }


    
}
