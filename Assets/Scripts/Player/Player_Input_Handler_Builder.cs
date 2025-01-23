using UnityEngine;

public class Player_Input_Handler_Builder : Player_Input_Handler
{
    private PlayerControllerBuilder builderControllerScript;

    protected override void Awake()
    {
        base.Awake();
        builderControllerScript = GetComponent<PlayerControllerBuilder>();
    }

    protected override void Update()
    {
        base.Update();

        if (Input.GetKeyUp(KeyCode.B))
        {
            
            Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::UPDATE:: Calling SetBuildMode (the old way)");
            builderControllerScript.ToggleBuildMode();
            
        }

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
            Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONENABLE:: Subscribing to Select.performed...");
            m_PlayerActions.Player.Select.performed += OnLeftClickPerformed;
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
        Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONENABLE:: Unsubscribing from Select.performed...");
        m_PlayerActions.Player.Select.performed -= OnLeftClickPerformed;

        // Disable player actions
        m_PlayerActions.Disable();
        base.OnDisable();
    }

    private void OnBuildModePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!IsOwner || !Application.isFocused)
        {
            return;
        } 

        Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONBUILDMODEPERFORMED:: Executing OnBuildModePerformed callback");
        builderControllerScript.ToggleBuildMode();
    }

    private void OnLeftClickPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!IsOwner || !Application.isFocused)
        {
            return;
        }

        Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONLEFTCLICKPERFORMED:: Executing OnLeftClickerPerformed callback");

        // Call the build function if the player can build
        if (builderControllerScript.GetCanBuild())
        {
            Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONLEFTCLICKPERFORMED:: Player can build.. calling StartingBuild function");
            builderControllerScript.StartBuild();
        } 

        else
        {
            Debug.Log("PLAYER_INPUT_HANDLER_BUILDER::ONLEFTCLICKPERFORMED:: Player cannot build");
        }
        
    }

    protected override void FlipSpriteRpc()
    {
        base.FlipSpriteRpc();
        builderControllerScript.FlipBuildSprite(m_isFacingRight);  

    }
}
