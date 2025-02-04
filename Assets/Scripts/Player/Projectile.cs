using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float m_projectileSpeed = 12f;
    [SerializeField] private float m_damage = 40f;
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
        Debug.Log("Collision!");

        if (collision.gameObject.CompareTag("Resources"))
        {
            Debug.Log("Projectile collided with a resource box!");
            DespawnProjectile();
        }

        else if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Projectile collided with Enemy!");
            DespawnProjectile();
            HealthComponent healthComponent = collision.gameObject.GetComponentInParent<HealthComponent>();
            healthComponent.RemoveHealth(m_damage);
        }
    }

    [Rpc(SendTo.NotServer)]
    public void SetDirectionRpc(Vector2 fireDirection)
    {
        SetDirection(fireDirection);
    }

    public void SetDirection(Vector2 fireDirection)
    {
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    private void MoveProjectile()
    {
        m_RB.linearVelocity = transform.up * m_projectileSpeed;
    }

    void DespawnProjectileAfterLifespan()
    {
        Debug.Log("Start the despawn lifespan timer");
        StartCoroutine(LifespanTimer());
    }

    void DespawnProjectile()
    {
        Debug.Log("Despawn the projectile");

        GetComponent<NetworkObject>().Despawn();

        if (GetComponent<NetworkObject>() == null)
        {
            Debug.Log("Network object is null on projectile");
        }
    }

    IEnumerator LifespanTimer()
    {
        Debug.Log("Despawn lifespan timer started...");
        yield return new WaitForSeconds(m_lifespan);
        DespawnProjectile();
    }
}