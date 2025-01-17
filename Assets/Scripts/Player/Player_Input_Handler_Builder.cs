using UnityEngine;

public class Player_Input_Handler_Builder : Player_Input_Handler
{
    private PlayerControllerBuilder builderControllerScript;

    protected override void Awake()
    {
        base.Awake();
        builderControllerScript = GetComponent<PlayerControllerBuilder>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (m_PlayerActions != null)
        {
            m_PlayerActions.Enable();
            Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONENABLE:: Subscribing to BuildMode.performed...");
            m_PlayerActions.Player.BuildMode.performed += OnBuildModePerformed;
            Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONENABLE:: Subscription successful.");
        }

        else
        {
            Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONENABLE:: Subscription unsuccessful.. m_PlayerActions is null.");
        }
            
    }
    protected override void OnDisable()
    {
        Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONDISABLE:: Unsubscribing from BuildMode.performed...");
        m_PlayerActions.Player.BuildMode.performed -= OnBuildModePerformed;
        m_PlayerActions.Disable();
        base.OnDisable();
    }

    private void OnBuildModePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONBUILDMODEPERFORMED:: Executing OnBuildModePerformed callback");
        builderControllerScript.SetBuildMode();
    }

}
