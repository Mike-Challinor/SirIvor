using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float m_projectileSpeed = 12f;
    [SerializeField] private float m_damage = 100f;
    [SerializeField] private float m_lifespan = 4f;
    private Rigidbody2D m_RB;

    public override void OnNetworkSpawn()
    {
        Debug.Log("On network spawn started");

        DespawnProjectileAfterLifespan();
        m_RB = GetComponent<Rigidbody2D>();
        m_RB.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Update()
    {
        MoveProjectile();
    }

    // Detect collision with walls
    void OnCollisionEnter2D(Collision2D collision)
    {

        // Despawning logic on collision
        if (collision.gameObject.CompareTag("Resources"))
        {
            DespawnProjectile();
        }

        else if (collision.gameObject.CompareTag("Enemy"))
        {
            DespawnProjectile();
            HealthComponent healthComponent = collision.gameObject.GetComponentInParent<HealthComponent>();
            healthComponent.RemoveHealth(m_damage);
        }

        else if (collision.gameObject.CompareTag("Player"))
        {
            DespawnProjectile();
        }
    }

    // Client Rpc to set direction of the projectile
    [Rpc(SendTo.NotServer)]
    public void SetDirectionRpc(Vector2 fireDirection)
    {
        SetDirection(fireDirection);
    }

    // Method to set direction of the projectile
    public void SetDirection(Vector2 fireDirection)
    {
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    // Method for moving the projectile
    private void MoveProjectile()
    {
        m_RB.linearVelocity = transform.up * m_projectileSpeed;
    }

    // Method for despawning the projectile at the end of its lifespan
    void DespawnProjectileAfterLifespan()
    {
        StartCoroutine(LifespanTimer());
    }

    // Method for despawning the projectile
    void DespawnProjectile()
    {
        GetComponent<NetworkObject>().Despawn();

        if (GetComponent<NetworkObject>() == null)
        {
            Debug.Log("Network object is null on projectile");
        }
    }

    // Timer method for the lifespan of projectile
    IEnumerator LifespanTimer()
    {
        yield return new WaitForSeconds(m_lifespan);
        DespawnProjectile();
    }
}