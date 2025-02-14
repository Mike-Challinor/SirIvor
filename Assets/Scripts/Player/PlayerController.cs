using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private NetworkTransform m_playerTransform;
    [SerializeField] protected PlayerHUD m_playerHUD;
    [SerializeField] protected CanvasGroup m_playerHUDCanvasGroup;
    [SerializeField] protected Camera m_mainCamera;
    [SerializeField] protected Camera m_playerCamera;
    [SerializeField] protected Player_Input_Handler m_playerInputHandler;
    [SerializeField] private float m_moveSpeed = 5f;
    [SerializeField] private float m_fadeDuration = 1.5f;
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
                m_playerHUD.initHUD(500f);
                StartCoroutine(ShowHUD());
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

    private IEnumerator ShowHUD()
    {
        // Delay before starting fade in
        yield return new WaitForSeconds(1);

        // Set the game object to be active
        m_playerHUD.SetHUDStatus(true);

        // Reset elapsed time variable
        float elapsedTime = 0f;

        // Loop through for fade duration
        while (elapsedTime < m_fadeDuration)
        {
            // Lerp the alpha of the canvas group over the specified time
            m_playerHUDCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / m_fadeDuration);

            // Increment the elapsed time using Delta time
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        // Ensure it's fully visible at the end of the coroutine
        m_playerHUDCanvasGroup.alpha = 1f; 
    }


    // Function for checking whether the mouse position is within the screen bounds
    protected bool IsMouseWithinScreen()
    {
        Vector3 mousePos = Input.mousePosition;
        return mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height;
    }

    // Accessor method for getting movement speed
    public float GetMoveSpeed()
    {
        return m_moveSpeed;
    }

    // Method for setting the wave timer on the playerHUD
    public void SetWaveTimer(int count)
    {
        if (IsOwner)
        {
            // Convert the wave timer to a string and update on the playerHUD
            m_playerHUD.SetWaveTimer(count.ToString());
        }
        
    }

    // Method for setting the wave count on the PlayerHUD
    public void SetWaveCount(int count)
    {
        if (IsOwner)
        {
            // Convert the wave count to a string and update on the playerHUD
            m_playerHUD.SetWaveCount(count.ToString());
        }
    }

    public void FadeInWaveCount()
    {
        m_playerHUD.FadeInWaveCount();
    }

    public void FadeWaveTimer(bool fadeIn)
    {
        m_playerHUD.FadeWaveTimer(fadeIn);
    }

}
