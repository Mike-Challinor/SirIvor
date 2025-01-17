using UnityEngine;
using System.Collections;

public class PlayerControllerBuilder : PlayerController
{
    bool m_isBuildMode = false;
    bool m_isBuilding = false;
    float m_buildTimer = 1f;

    Sprite m_equippedSprite;
    [SerializeField] Sprite[] m_SpriteArray;
    [SerializeField] GameObject m_buildSprite;
    SpriteRenderer m_SpriteComponent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        Debug.Log("PLAYERCONTROLLERBUILDER::START:: Function called before base start...");

        base.Start(); // Call the start function on the base class

        Debug.Log("PLAYERCONTROLLERBUILDER::START:: Function called after base start...");

        if (m_SpriteArray.Length != 0) // Check that the sprite array is not empty
        {
            m_equippedSprite = m_SpriteArray[0]; // Set the equipped sprite to the first element in the array
        }

        if (m_buildSprite != null) { m_SpriteComponent = m_buildSprite.GetComponent<SpriteRenderer>(); } // Set the sprite component

        if (m_equippedSprite != null) { SetSprite(m_equippedSprite); } // Set the sprite

    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update(); // Call the update function on the base class        
    }

    public void SetBuildMode()
    {
        m_isBuildMode = !m_isBuildMode; // Set build mode

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
        // Draw sprite in front of player
        if (m_buildSprite != null) // Check sprite component is not null
        {
            m_buildSprite.SetActive(true); // Enable the sprite component
        }

        else
        {
            Debug.Log("PLAYERCONTROLLERBUILDER::ENABLEBUILDMODE:: Sprite component is null");
        }
    }
    
    public void DisableBuildMode()
    {
        // Remove sprite in front of player
        if (m_buildSprite != null) // Check sprite component is not null
        {
            m_buildSprite.SetActive(false); // Disable the sprite component
        }

        else
        {
            Debug.Log("PLAYERCONTROLLERBUILDER::DISABLEBUILDMODE:: Sprite component is null");
        }
    }

    private IEnumerator StartBuild()
    {
        if (!m_isBuilding)
        {
            m_isBuilding = true;
            yield return StartCoroutine(BuildingTimer()); // Wait for building timer to finish before continuing
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

    private void SetSprite(Sprite sprite)
    {
        if (m_SpriteComponent != null) { m_SpriteComponent.sprite = sprite; }
    }

}
