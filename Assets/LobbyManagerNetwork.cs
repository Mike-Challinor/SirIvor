using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Security.Cryptography;

public class LobbyManagerNetwork : MonoBehaviour
{
    [SerializeField] private Button m_builderButton;
    [SerializeField] private Button m_shooterButton;
    [SerializeField] private Button m_builderReadyButton;
    [SerializeField] private Button m_shooterReadyButton;

    NetworkVariable<int> m_readyCount = new NetworkVariable<int>();
    NetworkVariable<bool> m_builderSelectedServer = new NetworkVariable<bool>();
    NetworkVariable<bool> m_shooterSelectedServer = new NetworkVariable<bool>();

    [SerializeField] private bool m_builderSelected = false;
    [SerializeField] private bool m_shooterSelected = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_builderSelectedServer.OnValueChanged += Handle_BuilderClassSelected_OnValueChanged;
        m_shooterSelectedServer.OnValueChanged += Handle_ShooterClassSelected_OnValueChanged;

        m_builderButton.onClick.AddListener(() =>
        {
            Debug.Log("Builder button clicked.");
            m_builderSelectedServer.Value = true;

            if (m_shooterSelectedServer.Value)
            {
                m_shooterSelected = false;
                m_shooterSelectedServer.Value = false;
            }

            m_builderSelected = true;

        });

        m_shooterButton.onClick.AddListener(() =>
        {
            Debug.Log("Shooter button clicked. Starting client...");
            m_shooterSelectedServer.Value = true;

            if (m_builderSelectedServer.Value)
            {
                m_builderSelected = false;
                m_builderSelectedServer.Value = false;
            }

            m_shooterSelected = true;
        });

        m_builderReadyButton.onClick.AddListener(() =>
        {
            Debug.Log("Builder ready button clicked.");

        });

        m_shooterReadyButton.onClick.AddListener(() =>
        {
            Debug.Log("Shooter ready button clicked.");
        });
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

    [Rpc(SendTo.ClientsAndHost)]
    void SelectClassRpc(int Class)
    {

    }
}
