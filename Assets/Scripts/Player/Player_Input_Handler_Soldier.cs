using UnityEngine;

public class Player_Input_Handler_Soldier : Player_Input_Handler
{
    [SerializeField] private PlayerControllerSoldier m_soldierControllerScript;

    protected override void Awake()
    {
        base.Awake();
        m_soldierControllerScript = GetComponent<PlayerControllerSoldier>();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (m_PlayerActions != null)
        {
            m_PlayerActions.Enable();
            Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONENABLE:: Subscribing to Interact.performed...");
            m_PlayerActions.Player.Interact.performed += OnInteractPerformed;
            Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONENABLE:: Subscription successful.");
            Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONENABLE:: Subscribing to Select.performed...");
            m_PlayerActions.Player.Select.performed += OnLeftClickPerformed;
            Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONENABLE:: Subscription successful.");
        }

        else
        {
            Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONENABLE:: Subscription unsuccessful.. m_PlayerActions is null.");
        }
            
    }
    protected override void OnDisable()
    {
        Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONDISABLE:: Unsubscribing from BuildMode.performed...");
        m_PlayerActions.Player.Interact.performed -= OnInteractPerformed;
        Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONENABLE:: Unsubscribing from Select.performed...");
        m_PlayerActions.Player.Select.performed -= OnLeftClickPerformed;

        // Disable player actions
        m_PlayerActions.Disable();
        base.OnDisable();
    }

    private void OnInteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!IsOwner || !Application.isFocused)
        {
            return;
        }

        Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONINTERACTPERFORMED:: Executing OnInteractPerformed callback");

        if (m_soldierControllerScript.GetInteractStatus() && !m_soldierControllerScript.GetShootStatus())
        {
            // Set the player on top of the platform
            Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::UPDATE:: Calling SetShootMode");
            SetCanMove(false);
            m_soldierControllerScript.SetShootMode();
        }

        else if (!m_soldierControllerScript.GetInteractStatus() && m_soldierControllerScript.GetShootStatus())
        {
            // Remove player from the platform
            m_soldierControllerScript.SetShootMode();
            SetCanMove(true);
        }

    }

    private void OnLeftClickPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!IsOwner || !Application.isFocused)
        {
            return;
        }

        Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONLEFTCLICKPERFORMED:: Executing OnLeftClickPerformed callback");

        m_soldierControllerScript.Shoot();

    }

}
