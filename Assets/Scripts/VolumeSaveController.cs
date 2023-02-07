using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSaveController : MonoBehaviour
{
    [SerializeField] private Slider effectVolumeSlider = null;
    [SerializeField] private Slider musicVolumeSlider = null;

    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _effectSource;


    private void Start()
    {
        LoadValues();
    }

    public void SaveVolumeButton()
    {
        float effectVolumeValue = effectVolumeSlider.value;
        float musicVolumeValue = musicVolumeSlider.value;

        PlayerPrefs.SetFloat("EffectVolume", effectVolumeValue);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeValue);

        LoadValues();
    }

    void LoadValues()
    {
        float effectVolumeValue = PlayerPrefs.GetFloat("EffectVolume");
        float musicVolumeValue = PlayerPrefs.GetFloat("MusicVolume");
        effectVolumeSlider.value = effectVolumeValue;
        musicVolumeSlider.value = musicVolumeValue;

        _effectSource.volume = effectVolumeValue;
        _musicSource.volume = musicVolumeValue;
    }
}
