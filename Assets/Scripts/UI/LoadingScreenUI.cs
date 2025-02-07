using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreenUI : MonoBehaviour
{
    [SerializeField] GameObject loadingscreen;
    [SerializeField] Slider loadingSlider;
    [SerializeField] bool isLoading = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SimulateLoading());
    }

    // Coroutine to simulate a loading delay and update the slider smoothly
    private IEnumerator SimulateLoading()
    {
        isLoading = true;

        // Initialize slider
        if (loadingSlider != null)
        {
            loadingSlider.value = 0;
        }

        // Total loading time in seconds
        float loadingDuration = 5f;
        float elapsedTime = 0f;

        // Smoothly update the slider over the duration of the loading
        while (elapsedTime < loadingDuration)
        {
            elapsedTime += Time.deltaTime;  // Increment time by frame time

            // Update slider value
            if (loadingSlider != null)
            {
                loadingSlider.value = Mathf.Lerp(0f, 1f, elapsedTime / loadingDuration);
            }

            yield return null;  // Wait until the next frame
        }

        isLoading = false;
        loadingscreen.SetActive(false);

    }

    public bool GetIsLoading()
    {
        return isLoading;
    }    
}
