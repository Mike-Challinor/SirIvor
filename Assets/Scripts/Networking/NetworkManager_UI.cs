using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;

public class NetworkManager_UI : MonoBehaviour
{
    [SerializeField] private Button m_HostGameButton;
    [SerializeField] private Button m_JoinGameButton;
    [SerializeField] private GameObject m_MainMenuCanvas;
    [SerializeField] private GameObject m_LobbyCanvas;

    void Start()
    {
        m_HostGameButton.onClick.AddListener(() =>
        {
            Debug.Log("Host button clicked. Starting host...");
            NetworkManager.Singleton.StartHost();
            m_MainMenuCanvas.SetActive(false);
            m_LobbyCanvas.SetActive(true);
            
        });

        m_JoinGameButton.onClick.AddListener(() =>
        {
            m_MainMenuCanvas.SetActive(false);
            m_LobbyCanvas.SetActive(true);
            Debug.Log("Client button clicked. Starting client...");
            NetworkManager.Singleton.StartClient();
        });
    }

}
