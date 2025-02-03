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
            m_PlayerActions.Player.Interact.performed += OnInteractPerformed;
            m_PlayerActions.Player.Select.performed += OnLeftClickPerformed;
            m_PlayerActions.Player.Select.canceled += OnLeftClickReleased;
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

        else if (m_soldierControllerScript.GetShootStatus())
        {
            // Remove player from the platform
            m_soldierControllerScript.SetShootMode();
            SetCanMove(true);
        }

    }

    private void OnLeftClickPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!IsOwner || !Application.isFocused) { return; }

        if (m_soldierControllerScript == null)
        {
            Debug.LogError("PLAYER_INPUT_HANDLER_SOLDIER::m_soldierControllerScript is NULL!");
            return;
        }

        if (m_soldierControllerScript.GetShootStatus() && m_soldierControllerScript.GetCanAttack())
        {
            Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONLEFTCLICKPERFORMED:: Executing OnLeftClickPerformed callback");

            m_soldierControllerScript.InitiateAttack();
        }
    }

    private void OnLeftClickReleased(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!IsOwner || !Application.isFocused) { return; }

        Debug.Log("PLAYER_INPUT_HANDLER_SOLDIER::ONLEFTCLICKRELEASED:: Executing OnLeftClickReleased callback");

        m_soldierControllerScript.EndAttack();
    }

}
