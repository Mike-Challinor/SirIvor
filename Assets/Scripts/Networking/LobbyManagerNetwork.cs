using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class LobbyManagerNetwork : NetworkBehaviour
{
    [SerializeField] private NetworkManager m_networkManager;
    [SerializeField] private NetworkManager_GameManager m_gameManager;
    [SerializeField] private NetworkManager_GameManager.PlayerClass m_playerClass;
    [SerializeField] private Button m_builderButton;
    [SerializeField] private Button m_shooterButton;
    [SerializeField] private Button m_builderReadyButton;
    [SerializeField] private Button m_shooterReadyButton;
    [SerializeField] private TMP_Text m_timerText;
    [SerializeField] private TMP_Text m_timerText2;
    [SerializeField] private Image m_playerOneCounter;
    [SerializeField] private Image m_playerTwoCounter;

    NetworkVariable<int> m_readyCount = new NetworkVariable<int>(0);
    NetworkVariable<int> m_timerCount = new NetworkVariable<int>(5);
    NetworkVariable<bool> m_builderSelectedServer = new NetworkVariable<bool>(false);
    NetworkVariable<bool> m_shooterSelectedServer = new NetworkVariable<bool>(false);

    [SerializeField] private bool m_builderSelected = false;
    [SerializeField] private bool m_shooterSelected = false;
    [SerializeField] private bool m_isReadied = false;
    [SerializeField] private ulong m_clientId;

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        m_gameManager = m_networkManager.GetComponent<NetworkManager_GameManager>();
        m_playerClass = NetworkManager_GameManager.PlayerClass.Default;

        m_builderSelectedServer.OnValueChanged += Handle_BuilderClassSelected_OnValueChanged;
        m_shooterSelectedServer.OnValueChanged += Handle_ShooterClassSelected_OnValueChanged;
        m_readyCount.OnValueChanged += Handle_ReadyCount_OnValueChanged;
        m_timerCount.OnValueChanged += Handle_TimerCount_OnValueChanged;

        m_builderButton.onClick.AddListener(() =>
        {
            Debug.Log("Builder button clicked.");

            SetBuilderSelectedRpc(true, m_clientId);

            if (m_shooterSelected == true)
            {
                m_shooterSelected = false;
                SetShooterSelectedRpc(false, m_clientId);
                m_shooterReadyButton.interactable = false;
            }

            m_builderReadyButton.interactable = true;
            m_builderSelected = true;

            UpdatePlayerCounterPositionServerRpc(-500, m_clientId);
        });

        m_shooterButton.onClick.AddListener(() =>
        {
            Debug.Log("Shooter button clicked. Starting client...");

            SetShooterSelectedRpc(true, m_clientId);

            if (m_builderSelected == true)
            {
                m_builderSelected = false;
                SetBuilderSelectedRpc(false, m_clientId);
                m_builderReadyButton.interactable = false;
            }

            m_shooterReadyButton.interactable = true;
            m_shooterSelected = true;

            UpdatePlayerCounterPositionServerRpc(500, m_clientId);
        });

        m_builderReadyButton.onClick.AddListener(() =>
        {
            Debug.Log("Builder ready button clicked.");

            // Disable all buttons
            m_shooterButton.interactable = false;
            m_shooterReadyButton.interactable = false;
            m_builderReadyButton.interactable = false;

            // Set the player's class
            m_playerClass = NetworkManager_GameManager.PlayerClass.Builder;

            // Let the server update the player list
            SetPlayerClassOnServerRpc(m_clientId, m_playerClass);

            // Update the amount of readied players on the server
            UpdateReadiedPlayersRpc(true);
        });

        m_shooterReadyButton.onClick.AddListener(() =>
        {
            Debug.Log("Shooter ready button clicked.");

            // Disable all buttons
            m_builderButton.interactable = false;
            m_builderReadyButton.interactable = false;
            m_shooterReadyButton.interactable = false;

            // Set the player's class
            m_playerClass = NetworkManager_GameManager.PlayerClass.Shooter;

            // Let the server update the player list
            SetPlayerClassOnServerRpc(m_clientId, m_playerClass);

            // Update the amount of readied players on the server
            UpdateReadiedPlayersRpc(true);
        });
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            m_clientId = clientId;
            Debug.Log($"Client {clientId} connected and assigned network ID: {m_clientId}");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(1))
        {
            Deselect();
        }
    }

    private void Handle_BuilderClassSelected_OnValueChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            m_builderButton.interactable = false;
        }

        else
        {
            m_builderButton.interactable = true;
        }
    }

    private void Handle_ShooterClassSelected_OnValueChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            m_shooterButton.interactable = false;
        }

        else
        {
            m_shooterButton.interactable = true;
        }
    }

    private void Handle_ReadyCount_OnValueChanged(int previousValue, int newValue)
    {
        if (newValue == 2)
        {
            m_timerText.gameObject.SetActive(true);
            m_timerText2.gameObject.SetActive(true);
        }
    }

    private void Handle_TimerCount_OnValueChanged(int previousValue, int newValue)
    {
        m_timerText.SetText(newValue.ToString());
    }

    private void Deselect()
    {
        if (!m_isReadied)
        {
            if (m_builderSelected)
            {
                m_builderSelected = false;
                SetBuilderSelectedRpc(false, m_clientId);
                m_builderReadyButton.interactable = false;

                if (m_shooterSelectedServer.Value == false)
                {
                    m_shooterButton.interactable = true;
                }
            }

            else if (m_shooterSelected)
            {
                m_shooterSelected = false;
                SetShooterSelectedRpc(false, m_clientId);
                m_shooterReadyButton.interactable = false;

                if (m_builderSelectedServer.Value == false)
                {
                    m_builderButton.interactable = true;
                }
            }

            else
            {
                Debug.Log("Nothing to deselect");
            }

            UpdatePlayerCounterPositionServerRpc(0, m_clientId);
        }
    }

    [Rpc(SendTo.Server)]
    private void SetPlayerClassOnServerRpc(ulong clientId, NetworkManager_GameManager.PlayerClass chosenClass)
    {
        // Let the server handle adding the player
        m_gameManager.AddPlayerRpc(clientId, chosenClass);
    }

    [Rpc(SendTo.Server)]
    private void SetBuilderSelectedRpc(bool isSelected, ulong clientId)
    {
        m_builderSelectedServer.Value = isSelected;

        Debug.Log($"Builder selected network variable has been set to: {m_builderSelectedServer.Value} by player {clientId}");
    }

    [Rpc(SendTo.Server)]
    private void SetShooterSelectedRpc(bool isSelected, ulong clientId)
    {
        m_shooterSelectedServer.Value = isSelected;

        Debug.Log($"Shooter selected network variable has been set to: {m_builderSelectedServer.Value} by player {clientId}");
    }

    [Rpc(SendTo.Server)]
    private void UpdateReadiedPlayersRpc(bool isReady)
    {
        if (isReady)
        {
            m_readyCount.Value++;

            if (m_readyCount.Value == 2)
            {
                StartCoroutine(StartTimer());
            }
        }

        else
        {
            m_readyCount.Value--;
        }

        Debug.Log($"Ready count is now {m_readyCount.Value}");
    }

    [Rpc(SendTo.Server)]
    private void UpdatePlayerCounterPositionServerRpc(int amountToChange, ulong clientId)
    {
        Debug.Log($"Updating player counter on player {m_clientId}");

        if (clientId == 0)
        {
            m_playerOneCounter.rectTransform.anchoredPosition = new Vector2(0 + amountToChange, m_playerOneCounter.rectTransform.anchoredPosition.y);
        }

        else
        {
            m_playerTwoCounter.rectTransform.anchoredPosition = new Vector3(0 + amountToChange, m_playerTwoCounter.rectTransform.anchoredPosition.y, 0);
        }

        UpdatePlayerCounterPositionClientRpc(amountToChange, clientId);
    }

    [Rpc(SendTo.NotServer)]
    private void UpdatePlayerCounterPositionClientRpc(int amountToChange, ulong clientId)
    {
        Debug.Log($"Updating player counter on player {clientId}");

        if (clientId == 0)
        {
            m_playerOneCounter.rectTransform.anchoredPosition = new Vector2(0 + amountToChange, m_playerOneCounter.rectTransform.anchoredPosition.y);
        }

        else
        {
            m_playerTwoCounter.rectTransform.anchoredPosition = new Vector3(0 + amountToChange, m_playerTwoCounter.rectTransform.anchoredPosition.y, 0);
        }
    }

    private IEnumerator StartTimer()
    {
        yield return StartCoroutine(GameStartTimer());

        // Start Game
        m_gameManager.StartGameRpc();
    }

    private IEnumerator GameStartTimer()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return StartCoroutine(TimerLength());
        }
    }

    private IEnumerator TimerLength()
    {
        yield return new WaitForSeconds(1);
        UpdateTimerCountRpc();
    }

    [Rpc(SendTo.Server)]
    private void UpdateTimerCountRpc()
    {
        m_timerCount.Value--;
    }
}
