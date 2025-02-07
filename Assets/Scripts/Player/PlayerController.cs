using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private NetworkTransform m_playerTransform;
    [SerializeField] protected PlayerHUD m_playerHUD;
    [SerializeField] private HealthComponent m_healthComponent;
    [SerializeField] protected Camera m_mainCamera;
    [SerializeField] protected Camera m_playerCamera;
    [SerializeField] protected Player_Input_Handler m_playerInputHandler;
    [SerializeField] private float m_moveSpeed = 5f;

    protected Rigidbody2D m_RB;

    private const float m_cameraMinZoom = 4f;
    private const float m_cameraMaxZoom = 7.5f;
    private SpriteRenderer m_playerSprite;

    public override void OnNetworkSpawn()
    {
        // Below is code to execute on both owner and not owner
        Debug.Log("Player Init function called");

        // Set name of the player prefab in the Unity editor
        this.name = $"Player {GetComponent<NetworkObject>().OwnerClientId}";

        //Get references 
        m_mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        m_playerCamera = GetComponentInChildren<Camera>();
        m_playerHUD = GetComponent<PlayerHUD>();
        m_healthComponent = GetComponent<HealthComponent>();
        m_playerSprite = GetComponent<SpriteRenderer>();
        m_playerInputHandler = GetComponent<Player_Input_Handler>();
        m_RB = GetComponent<Rigidbody2D>();

        // Turn off main camera for player
        m_mainCamera.enabled = false;

        // Set depth of the camera
        m_playerCamera.depth = 20;

        // Below is code to execute on only the owner
        if (IsOwner)
        {
            // Initialise the player hud
            if (m_playerHUD == null)
            {
                Debug.Log("ERROR::PLAYERCONTROLLER::START:: Is Local player but Player HUD is null");
            }

            else
            {
                Debug.Log("PLAYERCONTROLLER::START:: Is local player and PlayerHud is not null");

                // Inititalise the player HUD
                m_playerHUD.initHUD(m_healthComponent.GetMaxHealth());

                // Enable the player hud
                m_playerHUD.SetHUDStatus(true);
            }
        }

        if (IsOwner) return; // Below is code to execute if not the owner

        // Disable the player camera if not owner
        m_playerCamera.enabled = false;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        //Only update if owner
        if(IsOwner)
        {
            //Call input function
            HandleInput();
        }
        
    }

    protected void HandleInput()
    {
        
    }


    // Function for checking whether the mouse position is within the screen bounds
    protected bool IsMouseWithinScreen()
    {
        Vector3 mousePos = Input.mousePosition;
        return mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height;
    }

    //Accessor method for getting movement speed
    public float GetMoveSpeed()
    {
        return m_moveSpeed;
    }

}
