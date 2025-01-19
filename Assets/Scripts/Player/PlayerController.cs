using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private NetworkTransform m_playerTransform;
    [SerializeField] private PlayerHUD m_playerHUD;
    [SerializeField] private HealthComponent m_healthComponent;
    [SerializeField] private Camera m_mainCamera;
    [SerializeField] protected Camera m_playerCamera;
    
    private const float m_cameraMinZoom = 4f;
    private const float m_cameraMaxZoom = 7.5f;
    private SpriteRenderer m_playerSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        // Below is code to execute on both owner and not owner

        // Set name of the player prefab in the Unity editor
        this.name = $"Player {GetComponent<NetworkObject>().OwnerClientId}";

        //Get references 
        m_mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        m_playerCamera = GetComponentInChildren<Camera>();
        m_playerHUD = GetComponent<PlayerHUD>();
        m_healthComponent = GetComponent<HealthComponent>();
        m_playerSprite = GetComponent<SpriteRenderer>();

        // Turn off main camera for player
        m_mainCamera.enabled = false;

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

        // Get the position of the players cursor 
        if (m_playerCamera == null)
        {
            Debug.Log("PLAYERCONTROLLER::HANDLEINPUT:: Player camera is null");
        }

        else
        {
            // Get mouse position
            Vector3 mousePos = m_playerCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0; // Ensure z is at the same level as the player

            // Check if the mouse is within the screen boundaries
            if (IsMouseWithinScreen() && Time.timeScale == 1)
            {
                // Calculate direction to mouse
                Vector3 direction = (mousePos - transform.position).normalized;

                // Calculate the rotation angle in radians
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            }
        }


        // Control camera zoom
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            // Zoom in (decrease orthographic size)
            m_playerCamera.orthographicSize = Mathf.Clamp(m_playerCamera.orthographicSize - 0.5f, m_cameraMinZoom, m_cameraMaxZoom);
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            // Zoom out (increase orthographic size)
            m_playerCamera.orthographicSize = Mathf.Clamp(m_playerCamera.orthographicSize + 0.5f, m_cameraMinZoom, m_cameraMaxZoom);
        }
    }


    // Function for checking whether the mouse position is within the screen bounds
    protected bool IsMouseWithinScreen()
    {
        Vector3 mousePos = Input.mousePosition;
        return mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height;
    }

}
