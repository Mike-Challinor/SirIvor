using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthbarUI : MonoBehaviour
{
    [SerializeField] private Slider slider;

    public void UpdateHealth(float current_health)
    {
        Debug.Log("HealthBar::UpdateHealth:: Update health called");
        slider.value = slider.maxValue - current_health;
    }
    public void InitSlider(float max_health)
    {
        slider.maxValue = max_health;
        slider.value = 0;
    }
}
