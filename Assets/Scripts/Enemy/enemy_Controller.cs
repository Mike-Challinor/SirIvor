using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class enemy_Controller : NetworkBehaviour
{
    [SerializeField] private GameObject[] m_allTargets;
    [SerializeField] private float m_moveSpeed = 1f;
    [SerializeField] private float m_attackDamage = 5f;
    [SerializeField] private float m_attackRange = 2f;
    [SerializeField] private float m_attackDuration = 1f;
    [SerializeField] private float m_attackCooldown = 2f;
    [SerializeField] private GameObject m_target;
    [SerializeField] private BoxCollider2D m_attackCollider;
    [SerializeField] private bool m_isAttacking = false;

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
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        // Reset move direction
        Vector2 moveDir = Vector2.zero;

        // Look for a target if the enemy does not have one
        if (m_target == null)
        {
            Debug.Log("ENEMY_CONTROLLER::UPDATE:: Enemy has no target..  Calling FindTarget() function...");
            FindTarget();
        }
        else
        {
            // Set the enemy's move direction towards the target
            moveDir = m_target.transform.position - transform.position;

            // Normalize the move direction
            moveDir.Normalize();

            // Calculate distance from target
            float distance = Vector2.Distance(m_target.transform.position, transform.position);

            if (distance > m_attackRange)
            {
                // Move the enemy towards the target
                m_RB.linearVelocity = moveDir * m_moveSpeed;

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

        // Reset the temporary target variable
        GameObject tempTarget = null;

        // Find and store reference to all players and convert to a List for easier manipulation
        List<GameObject> allTargets = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));

        // Remove any dead players from the list
        allTargets.RemoveAll(player =>
        {
            HealthComponent health = player.GetComponent<HealthComponent>();
            return health == null || health.GetHealth() <= 0; // Remove if player is dead or health component is missing
        });

        // Loop through all potential targets
        foreach (GameObject target in allTargets)
        {
            // If this is the first object in the list, set it as the temp target
            if (tempTarget == null)
            {
                tempTarget = target;
            }
            else
            {
                // Use sqrMagnitude to get float distances
                float tempTargetDistance = (tempTarget.transform.position - transform.position).sqrMagnitude;
                float foundTargetDistance = (target.transform.position - transform.position).sqrMagnitude;

                // Check if the found target's distance is less than the current temp target
                if (foundTargetDistance < tempTargetDistance)
                {
                    // Update the temp target to the closer found target
                    tempTarget = target;
                }
            }
        }

        // Set the enemy's target
        m_target = tempTarget;

        if (m_target != null)
        {
            Debug.Log("ENEMY_CONTROLLER::FINDTARGET:: Target found: " + m_target.name);
        }
        else
        {
            Debug.Log("ENEMY_CONTROLLER::FINDTARGET:: No valid targets found.");
        }
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
            if (collider.CompareTag("Player")) // Ensure the collider belongs to the player
            {
                HealthComponent playerHealth = collider.GetComponentInParent<HealthComponent>();

                if (playerHealth != null)
                {
                    Debug.Log("ENEMYCONTROLLER::ATTACKTARGETRPC:: Applying damage on the server");

                    // Apply damage on the server
                    playerHealth.RemoveHealth(m_attackDamage);

                    Debug.Log("ENEMYCONTROLLER::ATTACKTARGETRPC:: Calling UpdateHealthRpc...");
                    Debug.Log($"ENEMYCONTROLLER::ATTACKTARGETRPC:: Received NetworkObjectId: {collider.GetComponentInParent<NetworkObject>().NetworkObjectId}");

                    // Notify clients of the health change
                    UpdateHealthRpc(collider.GetComponentInParent<NetworkObject>().NetworkObjectId, playerHealth.GetHealth());
                }

                else
                {
                    Debug.Log("ENEMYCONTROLLER::ATTACKTARGETRPC:: Health component is null");
                }
            }
        }

        // Call attack cooldown after applying damage
        StartCoroutine(AttackCooldown());
    }

    // ClientRpc to update health on all clients
    [Rpc(SendTo.NotServer)]
    private void UpdateHealthRpc(ulong targetNetworkObjectId, float newhealth)
    {
        Debug.Log("ENEMYCONTROLLER::UPDATEHEALTHRPC:: Called UpdateHealthRPC() on clients... right?");

        // Find the target object using its NetworkObjectId
        var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetNetworkObjectId];

        // Check that the target has been found
        if (targetObject != null)
        {
            HealthComponent healthComponent = targetObject.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.SetHealth(newhealth);
            }
        }

        else
        {
            Debug.Log("ENEMYCONTROLLER::UPDATEHEALTHRPC:: Target object could not be found from Network Object ID");
        }
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
        ChangeSpriteColourRpc(Color.white); // Return sprite to white colour
        yield return new WaitForSeconds(m_attackCooldown);
        m_isAttacking = false;
    }

    // Function for flipping the sprite
    [Rpc(SendTo.ClientsAndHost)]
    void FlipSpriteRpc()
    {
        m_isFacingRight = !m_isFacingRight;
        m_enemySprite.flipX = !m_enemySprite.flipX;

        // Move collider position to correct direction
        if (m_isFacingRight)
        {
            m_attackCollider.offset = new Vector2(Mathf.Abs(m_attackCollider.offset.x), m_attackCollider.offset.y);
        }
        else
        {
            m_attackCollider.offset = new Vector2(-Mathf.Abs(m_attackCollider.offset.x), m_attackCollider.offset.y);
        }
    }

    // Debug function for drawing gizmos of the enemy's attack size
    private void OnDrawGizmos()
    {
        if (m_attackCollider != null)
        {
            // Use the collider's center, width, and height for the Gizmo
            Vector2 attackCenter = m_attackCollider.bounds.center;
            Vector2 attackSize = new Vector2(m_attackCollider.bounds.size.x, m_attackCollider.bounds.size.y);

            ChangeSpriteColourRpc(Color.red);
            Gizmos.DrawWireCube(attackCenter, (Vector3)attackSize); // Cast to Vector3 for visualization
        }
    }

    
    private void ChangeSpriteColourRpc(Color colour)
    {
        m_enemySprite.color = colour;
    }


    // Function for despawning the enemy
    private void DespawnOnDeath()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }
}
