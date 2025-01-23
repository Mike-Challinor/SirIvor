using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{

    [SerializeField] private float m_health;
    [SerializeField] private float m_maxHealth = 100f;

    private PlayerHUD m_playerHUD;

    // Start is called before the first frame update
    void Start()
    {
        m_health = m_maxHealth;
        m_playerHUD = GetComponent<PlayerHUD>();
    }

    public void AddHealth(float amount)
    {
        // Check if health to add exceeds max health
        if (m_health + amount >= m_maxHealth)
        {
            // Set health to max health if health exceeds max
            m_health = m_maxHealth;
        }

        // Add health
        else
        {
            m_health += amount;
        }

        // Update player HUD if present
        if (m_playerHUD == null)
        {

        }

        else
        {
            m_playerHUD.updateHealth(m_health);
        }
    } 

    public void RemoveHealth(float amount)
    {
        Debug.Log("RemoveHealth function called");

        // Make sure health does not go lower than 0
        if (m_health - amount < 0)
        {
            m_health = 0;
        }
        else
        {
            m_health -= amount;
        }

        Debug.Log(amount + " health removed");

        // Update player HUD if present
        if (m_playerHUD == null)
        {
            Debug.Log("HEALTHCOMPONENT::START:: Player HUD is null");
        }

        else
        {
            m_playerHUD.updateHealth(m_health);
        }
    }

    public void SetHealth(float newHealth)
    {
        m_health = newHealth;

        // Update player HUD if present
        if (m_playerHUD == null)
        {
            Debug.Log("HEALTHCOMPONENT::START:: Player HUD is null");
        }

        else
        {
            m_playerHUD.updateHealth(m_health);
        }
    }

    public float GetHealth()
    {
        return m_health;
    }

    public float GetMaxHealth()
    {
        return m_maxHealth;
    }

    public void SetMaxHealth(float health)
    {
        m_maxHealth = health;
    }
}

