using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using TMPro;

public class PlayerHUD : NetworkBehaviour
{
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private Slider slider;
    [SerializeField] private Image m_reticle;
    [SerializeField] private TMP_Text m_waveTimerText;
    [SerializeField] private TMP_Text m_waveCountText;
    [SerializeField] private float m_fadeDuration = 1.5f;

    private bool isHUDActive = false;

    // Function for initialising the hud
    public void initHUD(float max_health)
    {
        Debug.Log("PLAYERHUD::INITHUD:: initHUD function called");

        // Init the slider with 500 max health (for the building)
        slider.GetComponent<HealthbarUI>().InitSlider(max_health);
    }

    // Function for updating the health on the slider
    public void updateHealth(float current_health)
    {
        slider.GetComponent<HealthbarUI>().UpdateHealth(current_health);
    }

    // Set the huds status
    public void SetHUDStatus(bool status)
    {
        Debug.Log("PLAYERHUD::SETHUDSTATUS:: SetHUDStatus() function called");

        // If owner set the hud to active
        if (IsOwner)
        {
            // Set bool that tracks the status of the HUD
            isHUDActive = status;

            // Call the set HUD active function
            SetHUDActive();
        }

    }

    // Function for setting the huds status
    private void SetHUDActive()
    {
        // Show or hide the HUD
        playerHUD.gameObject.SetActive(isHUDActive);
    }

    // Function for setting the reticle status
    public void SetReticleStatus(bool reticleActive)
    {
        m_reticle.gameObject.SetActive(reticleActive);
    }

    // Function for getting the reticle status
    public bool GetReticleStatus()
    {
        return m_reticle.gameObject.activeSelf;
    }

    // Function for setting the reticle position
    public void SetReticlePosition(Vector3 mousePos)
    {
        m_reticle.transform.position = mousePos;
    }

    // Function for setting the wave timer text
    public void SetWaveTimer(string newText)
    {
        m_waveTimerText.text = newText;
    }

    // Function for setting the wave count text
    public void SetWaveCount(string newText)
    {
        m_waveCountText.text = "Wave " + newText;
    }

    // Function for fading in the wave count text
    public void FadeInWaveCount()
    {
        StartCoroutine(FadeTextTimer(m_waveCountText, true));
    }

    // Function for fading the wave timer text
    public void FadeWaveTimer(bool fadeIn)
    {
        StartCoroutine(FadeTextTimer(m_waveTimerText, fadeIn));
    }

    // Timer for fading text
    private IEnumerator FadeTextTimer(TMP_Text text, bool fadeIn)
    {
        // Delay before starting fade in
        // yield return new WaitForSeconds(1);

        // Reset elapsed time variable
        float elapsedTime = 0f;

        // Loop through for fade duration
        while (elapsedTime < m_fadeDuration)
        {
            if (fadeIn)
            {
                // Lerp the alpha of the canvas group over the specified time
                text.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(0f, 1f, elapsedTime / m_fadeDuration);
            }

            else
            {
                // Lerp the alpha of the canvas group over the specified time
                text.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(1f, 0f, elapsedTime / m_fadeDuration);
            }

            // Increment the elapsed time using Delta time
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        // Ensure it's fully visible (or invisible) at the end of the coroutine
        if (fadeIn)
        {
            text.GetComponent<CanvasGroup>().alpha = 1f;
        }

        else 
        {
            text.GetComponent<CanvasGroup>().alpha = 0f;
        }
        
    }

    

}
