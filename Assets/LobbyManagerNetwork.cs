using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Collections;
using System.Security.Cryptography;

public class LobbyManagerNetwork : NetworkBehaviour
{
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
    [SerializeField] private ulong m_networkId;

    void Start()
    {
        m_builderSelectedServer.OnValueChanged += Handle_BuilderClassSelected_OnValueChanged;
        m_shooterSelectedServer.OnValueChanged += Handle_ShooterClassSelected_OnValueChanged;
        m_readyCount.OnValueChanged += Handle_ReadyCount_OnValueChanged;
        m_timerCount.OnValueChanged += Handle_TimerCount_OnValueChanged;

        m_builderButton.onClick.AddListener(() =>
        {
            Debug.Log("Builder button clicked.");

            SetBuilderSelectedRpc(true, GetComponent<NetworkObject>().OwnerClientId);

            if (m_shooterSelected == true)
            {
                m_shooterSelected = false;
                SetShooterSelectedRpc(false, GetComponent<NetworkObject>().OwnerClientId);
                m_shooterReadyButton.interactable = false;
                
            }

            m_builderReadyButton.interactable = true;
            m_builderSelected = true;

            UpdatePlayerCounterPositionServerRpc(-500, GetComponent<NetworkObject>().OwnerClientId);

        });

        m_shooterButton.onClick.AddListener(() =>
        {
            Debug.Log("Shooter button clicked. Starting client...");

            SetShooterSelectedRpc(true, GetComponent<NetworkObject>().OwnerClientId);

            if (m_builderSelected == true)
            {
                m_builderSelected = false;
                SetBuilderSelectedRpc(false, GetComponent<NetworkObject>().OwnerClientId);
                m_builderReadyButton.interactable = false;
            }

            m_shooterReadyButton.interactable = true;
            m_shooterSelected = true;

            UpdatePlayerCounterPositionServerRpc(500, GetComponent<NetworkObject>().OwnerClientId);

        });

        m_builderReadyButton.onClick.AddListener(() =>
        {
            Debug.Log("Builder ready button clicked.");
            m_shooterButton.interactable = false;
            m_shooterReadyButton.interactable = false;
            m_builderReadyButton.interactable = false;
            UpdateReadiedPlayersRpc(true);

        });

        m_shooterReadyButton.onClick.AddListener(() =>
        {
            Debug.Log("Shooter ready button clicked.");
            m_builderButton.interactable = false;
            m_shooterReadyButton.interactable = false;
            m_builderReadyButton.interactable = false;
            UpdateReadiedPlayersRpc(true);
        });

        m_networkId = GetComponent<NetworkObject>().NetworkObjectId;
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
        if (m_builderSelected)
        {
            m_builderSelected = false;
            SetBuilderSelectedRpc(false, m_networkId);
            m_builderReadyButton.interactable = false;

            if (m_shooterSelectedServer.Value == false)
            {
                m_shooterButton.interactable = true;
            }
        }

        else if (m_shooterSelected)
        {
            m_shooterSelected = false;
            SetShooterSelectedRpc(false, m_networkId);
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

        UpdatePlayerCounterPositionServerRpc(0, m_networkId);
    }

    [Rpc(SendTo.Server)]
    private void SetBuilderSelectedRpc(bool isSelected, ulong networkId)
    {
        m_builderSelectedServer.Value = isSelected;

        Debug.Log($"Builder selected network variable has been set to: {m_builderSelectedServer.Value} by player {networkId}");
    }

    [Rpc(SendTo.Server)]
    private void SetShooterSelectedRpc(bool isSelected, ulong networkId)
    {
        m_shooterSelectedServer.Value = isSelected;

        Debug.Log($"Shooter selected network variable has been set to: {m_builderSelectedServer.Value} by player {networkId}");
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
    private void UpdatePlayerCounterPositionServerRpc(int amountToChange, ulong networkId)
    {
        Debug.Log($"Updating player counter on player {networkId}");

        if (networkId == 0)
        {
            m_playerOneCounter.rectTransform.anchoredPosition = new Vector2(0 + amountToChange, m_playerOneCounter.rectTransform.anchoredPosition.y);
        }

        else
        {
            m_playerTwoCounter.rectTransform.anchoredPosition = new Vector3(0 + amountToChange, m_playerOneCounter.rectTransform.anchoredPosition.y, 0);
        }

        UpdatePlayerCounterPositionClientRpc(amountToChange, networkId);
    }

    [Rpc(SendTo.NotServer)]
    private void UpdatePlayerCounterPositionClientRpc(int amountToChange, ulong networkId)
    {
        Debug.Log($"Updating player counter on player {networkId}");

        if (networkId == 0)
        {
            m_playerOneCounter.rectTransform.anchoredPosition = new Vector2(0 + amountToChange, m_playerOneCounter.rectTransform.anchoredPosition.y);
        }

        else
        {
            m_playerTwoCounter.rectTransform.anchoredPosition = new Vector3(0 + amountToChange, m_playerOneCounter.rectTransform.anchoredPosition.y, 0);
        }
    }

    private IEnumerator StartTimer()
    {
        yield return StartCoroutine(GameStartTimer());
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
