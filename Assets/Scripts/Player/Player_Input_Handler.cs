using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.U2D;

public class Player_Input_Handler : NetworkBehaviour
{
    private Rigidbody2D m_RB;
    private Vector2 m_moveInput;
    private float m_camInput;
    private SpriteRenderer m_playerSprite;
    private Transform m_playerTransform;
    protected PlayerInputAction m_PlayerActions;
    private PlayerController m_playerController;
    private LoadingScreenUI m_loadingScreenUI;

    [SerializeField] protected bool m_isFacingRight = true;
    [SerializeField] private bool m_canMove = true;

    [SerializeField] private GameObject m_loadingScreen;

    protected virtual void Awake()
    {
        m_RB = GetComponent<Rigidbody2D>();
        m_playerSprite = GetComponent<SpriteRenderer>();
        m_playerTransform = GetComponent<Transform>();
        m_playerController = GetComponent<PlayerController>();
        m_loadingScreen = GameObject.FindGameObjectWithTag("LoadingScreen");
        m_loadingScreenUI = m_loadingScreen.GetComponent<LoadingScreenUI>();
    }

    protected virtual void OnEnable()
    {
        m_PlayerActions = new PlayerInputAction();

        m_PlayerActions.Enable();

        // Listen for movement input
        m_PlayerActions.Player.Move.performed += ctx => m_moveInput = ctx.ReadValue<Vector2>();
        // Stop movement on release
        m_PlayerActions.Player.Move.canceled += ctx => m_moveInput = Vector2.zero;

    }

    protected virtual void OnDisable()
    {
        m_PlayerActions.Player.Move.performed -= ctx => m_moveInput = ctx.ReadValue<Vector2>();
        m_PlayerActions.Player.Move.canceled -= ctx => m_moveInput = Vector2.zero;
        m_PlayerActions.Disable();
    }

    protected virtual void Update()
    {
        if (!IsOwner) return;

        if (m_loadingScreenUI.GetIsLoading())
        {
            // Do not move player as game is still loading
        }

        else
        {

            MovePlayer();
        }
    }

    private void MovePlayer()
    {
        if (m_canMove)
        {
            //Check to see if moving left or right
            if ((m_moveInput.x < 0 && m_isFacingRight) || (m_moveInput.x > 0 && !m_isFacingRight))
            {
                FlipSpriteRpc();
            }

            m_RB.linearVelocity = m_moveInput * m_playerController.GetMoveSpeed();

        }

    }

    [Rpc(SendTo.ClientsAndHost)]
    protected virtual void FlipSpriteRpc()
    {
        m_isFacingRight = !m_isFacingRight;
        m_playerSprite.flipX = !m_playerSprite.flipX;
    }

    public void SetCanMove(bool canMove)
    {
        m_canMove = canMove;
    }

}

