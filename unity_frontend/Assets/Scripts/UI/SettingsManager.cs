using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TensorHawks.Core;

/// <summary>
/// SettingsManager Bridge for Unity Inspector bindings.
/// Safe fallback script that forwards controls to GameManager.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("UI Controls")]
    public Slider volumeSlider;
    public TMP_Dropdown qualityDropdown;

    private void Start()
    {
        InitializeControls();
    }

    private void OnEnable()
    {
        InitializeControls();
    }

    private void InitializeControls()
    {
        float savedVol = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        AudioListener.volume = savedVol;

        if (volumeSlider != null)
        {
            volumeSlider.value = savedVol;
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.onValueChanged.RemoveAllListeners();
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }
    }

    public void SetVolume(float volume)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetVolume(volume);
        }
        else
        {
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat("MasterVolume", volume);
            PlayerPrefs.Save();
        }
    }

    public void SetQuality(int qualityIndex)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetQuality(qualityIndex);
        }
        else
        {
            QualitySettings.SetQualityLevel(qualityIndex, true);
        }
    }
}