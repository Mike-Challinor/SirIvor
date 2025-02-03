using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class PlayerHUD : NetworkBehaviour
{
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private Slider slider;
    [SerializeField] private Image m_reticle;
    private bool isHUDActive = false;

    public void initHUD(float max_health)
    {
        Debug.Log("PLAYERHUD::INITHUD:: initHUD function called");
        slider.GetComponent<HealthbarUI>().InitSlider(max_health);
    }

    public void updateHealth(float current_health)
    {
        slider.GetComponent<HealthbarUI>().UpdateHealth(current_health);
    }

    // Set the huds status
    public void SetHUDStatus(bool status)
    {
        Debug.Log("PLAYERHUD::SETHUDSTATUS:: SetHUDStatus() function called");

        if (IsOwner)
        {
            isHUDActive = status;
            SetHUDActive();
        }

    }

    private void SetHUDActive()
    {
        // Show or hide the HUD
        playerHUD.gameObject.SetActive(isHUDActive);
    }

    public void SetReticleStatus(bool reticleActive)
    {
        m_reticle.gameObject.SetActive(reticleActive);
    }

    public bool GetReticleStatus()
    {
        return m_reticle.gameObject.activeSelf;
    }

    public void SetReticlePosition(Vector3 mousePos)
    {
        m_reticle.transform.position = mousePos;
    }

}
