using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkManager_UI : MonoBehaviour
{
    [SerializeField] private Button m_ServerButton;
    [SerializeField] private Button m_HostButton;
    [SerializeField] private Button m_ClientButton;

    void Start()
    {

        m_ServerButton.onClick.AddListener(() =>
        {
            Debug.Log("Server button clicked. Starting server...");
            NetworkManager.Singleton.StartServer();
        });

        m_HostButton.onClick.AddListener(() =>
        {
            Debug.Log("Host button clicked. Starting host...");
            NetworkManager.Singleton.StartHost();
            
        });

        m_ClientButton.onClick.AddListener(() =>
        {
            Debug.Log("Client button clicked. Starting client...");
            NetworkManager.Singleton.StartClient();
        });
    }

}
