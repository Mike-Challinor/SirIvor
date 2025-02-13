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

    public void SetWaveTimer(string newText)
    {
        m_waveTimerText.text = newText;
    }

    public void SetWaveCount(string newText)
    {
        m_waveCountText.text = "Wave " + newText;
    }

    public void FadeInWaveCount()
    {
        //StartCoroutine(FadeTextTimer(m_waveCountText, true));
    }

    public void FadeWaveTimer(bool fadeIn)
    {
        //StartCoroutine(FadeTextTimer(m_waveTimerText, fadeIn));
    }

    private IEnumerator FadeTextTimer(TMP_Text text, bool fadeIn)
    {
        // Delay before starting fade in
        yield return new WaitForSeconds(1);

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
